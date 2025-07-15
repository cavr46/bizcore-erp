using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Sales;
using BizCore.Orleans.Core.Base;

namespace BizCore.Sales.Service.Grains;

public class SalesOrderGrain : TenantGrainBase<SalesOrderState>, ISalesOrderGrain
{
    public SalesOrderGrain(
        [PersistentState("salesOrder", "SalesStore")] IPersistentState<SalesOrderState> state)
        : base(state)
    {
    }

    public async Task<Result<Guid>> CreateAsync(CreateSalesOrderCommand command)
    {
        if (State.Id != Guid.Empty)
            return Result<Guid>.Failure("Sales order already exists");

        if (command.CustomerId == Guid.Empty)
            return Result<Guid>.Failure("Customer ID is required");

        if (command.Lines.Count == 0)
            return Result<Guid>.Failure("Sales order must have at least one line");

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync();

        State.Id = this.GetPrimaryKey();
        State.TenantId = command.TenantId;
        State.OrderNumber = orderNumber;
        State.OrderDate = command.OrderDate;
        State.CustomerId = command.CustomerId;
        State.CustomerName = ""; // Would be populated from customer grain
        State.CustomerPO = command.CustomerPO;
        State.Status = SalesOrderStatus.Draft;
        State.ShippingAddress = command.ShippingAddress;
        State.BillingAddress = command.BillingAddress;
        State.RequestedDeliveryDate = command.RequestedDeliveryDate;
        State.SalespersonId = command.SalespersonId;
        State.DiscountPercentage = 0;
        State.Notes = command.Notes;
        State.CreatedAt = DateTime.UtcNow;
        State.CreatedBy = command.CreatedBy;

        // Create lines
        State.Lines = new List<SalesOrderLine>();
        for (int i = 0; i < command.Lines.Count; i++)
        {
            var cmdLine = command.Lines[i];
            var line = new SalesOrderLine
            {
                LineNumber = i + 1,
                ProductId = cmdLine.ProductId,
                ProductSKU = "", // Would be populated from product grain
                ProductName = "", // Would be populated from product grain
                Quantity = cmdLine.Quantity,
                UnitOfMeasure = "EA",
                UnitPrice = cmdLine.UnitPrice ?? 0,
                DiscountPercentage = cmdLine.DiscountPercentage ?? 0,
                TaxRate = 0,
                WarehouseId = cmdLine.WarehouseId,
                Notes = cmdLine.Notes
            };

            // Calculate line total
            var lineSubtotal = line.Quantity * line.UnitPrice;
            var discountAmount = lineSubtotal * (line.DiscountPercentage / 100);
            line.LineTotal = lineSubtotal - discountAmount;

            State.Lines.Add(line);
        }

        // Calculate totals
        await RecalculateTotalsAsync();

        await WriteStateAsync();
        return Result<Guid>.Success(State.Id);
    }

    public async Task<Result> UpdateAsync(UpdateSalesOrderCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.Draft)
            return Result.Failure("Can only update draft orders");

        if (command.CustomerPO != null)
            State.CustomerPO = command.CustomerPO;

        if (command.ShippingAddress != null)
            State.ShippingAddress = command.ShippingAddress;

        if (command.BillingAddress != null)
            State.BillingAddress = command.BillingAddress;

        if (command.RequestedDeliveryDate.HasValue)
            State.RequestedDeliveryDate = command.RequestedDeliveryDate;

        if (command.Notes != null)
            State.Notes = command.Notes;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> AddLineAsync(SalesOrderLine line)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.Draft)
            return Result.Failure("Can only modify draft orders");

        line.LineNumber = State.Lines.Count + 1;
        
        // Calculate line total
        var lineSubtotal = line.Quantity * line.UnitPrice;
        var discountAmount = lineSubtotal * (line.DiscountPercentage / 100);
        line.LineTotal = lineSubtotal - discountAmount;

        State.Lines.Add(line);

        await RecalculateTotalsAsync();
        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateLineAsync(int lineNumber, SalesOrderLine line)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.Draft)
            return Result.Failure("Can only modify draft orders");

        var existingLine = State.Lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (existingLine == null)
            return Result.Failure("Line not found");

        existingLine.Quantity = line.Quantity;
        existingLine.UnitPrice = line.UnitPrice;
        existingLine.DiscountPercentage = line.DiscountPercentage;
        existingLine.Notes = line.Notes;

        // Recalculate line total
        var lineSubtotal = existingLine.Quantity * existingLine.UnitPrice;
        var discountAmount = lineSubtotal * (existingLine.DiscountPercentage / 100);
        existingLine.LineTotal = lineSubtotal - discountAmount;

        await RecalculateTotalsAsync();
        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> RemoveLineAsync(int lineNumber)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.Draft)
            return Result.Failure("Can only modify draft orders");

        var line = State.Lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line == null)
            return Result.Failure("Line not found");

        State.Lines.Remove(line);

        // Renumber remaining lines
        for (int i = 0; i < State.Lines.Count; i++)
        {
            State.Lines[i].LineNumber = i + 1;
        }

        await RecalculateTotalsAsync();
        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> SubmitForApprovalAsync(string submittedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.Draft)
            return Result.Failure("Order must be in draft status to submit for approval");

        if (State.Lines.Count == 0)
            return Result.Failure("Cannot submit order with no lines");

        State.Status = SalesOrderStatus.PendingApproval;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> ApproveAsync(string approvedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.PendingApproval)
            return Result.Failure("Order must be pending approval");

        State.Status = SalesOrderStatus.Approved;
        State.ApprovedBy = approvedBy;
        State.ApprovedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> RejectAsync(string rejectedBy, string reason)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.PendingApproval)
            return Result.Failure("Order must be pending approval");

        State.Status = SalesOrderStatus.Draft;
        State.Notes = $"{State.Notes}\n\nRejected by {rejectedBy}: {reason}";

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> ConvertToInvoiceAsync(string convertedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status != SalesOrderStatus.Approved)
            return Result.Failure("Order must be approved to convert to invoice");

        // In a real implementation, this would create an invoice
        State.Status = SalesOrderStatus.Invoiced;
        State.InvoiceId = Guid.NewGuid(); // Would be actual invoice ID

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> CancelAsync(string cancelledBy, string reason)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Sales order not found");

        if (State.Status == SalesOrderStatus.Completed || State.Status == SalesOrderStatus.Cancelled)
            return Result.Failure("Cannot cancel completed or already cancelled order");

        State.Status = SalesOrderStatus.Cancelled;
        State.Notes = $"{State.Notes}\n\nCancelled by {cancelledBy}: {reason}";

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result<SalesOrderTotals>> CalculateTotalsAsync()
    {
        if (State.Id == Guid.Empty)
            return Result<SalesOrderTotals>.Failure("Sales order not found");

        await RecalculateTotalsAsync();

        var totals = new SalesOrderTotals
        {
            SubTotal = State.SubTotal,
            DiscountAmount = State.SubTotal * (State.DiscountPercentage / 100),
            TaxableAmount = State.SubTotal,
            TaxAmount = State.TaxAmount,
            ShippingAmount = State.ShippingAmount,
            TotalAmount = State.TotalAmount,
            TaxBreakdown = new Dictionary<decimal, decimal>()
        };

        return Result<SalesOrderTotals>.Success(totals);
    }

    private async Task RecalculateTotalsAsync()
    {
        State.SubTotal = State.Lines.Sum(l => l.LineTotal);
        State.TaxAmount = State.SubTotal * 0.1m; // 10% tax rate for demo
        State.TotalAmount = State.SubTotal + State.TaxAmount + State.ShippingAmount;
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var date = DateTime.UtcNow;
        var datePrefix = date.ToString("yyyyMM");
        var random = new Random();
        var sequence = random.Next(1000, 9999);
        return $"SO-{datePrefix}-{sequence}";
    }
}