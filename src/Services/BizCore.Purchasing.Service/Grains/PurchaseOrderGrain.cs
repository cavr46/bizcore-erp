using BizCore.Purchasing.Domain.Entities;
using BizCore.Orleans.Core;
using Microsoft.Extensions.Logging;

namespace BizCore.Purchasing.Grains;

public interface IPurchaseOrderGrain : IGrainWithGuidKey
{
    Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request);
    Task<PurchaseOrder> GetPurchaseOrderAsync();
    Task AddLineAsync(AddPurchaseOrderLineRequest request);
    Task UpdateLineAsync(int lineNumber, UpdatePurchaseOrderLineRequest request);
    Task RemoveLineAsync(int lineNumber);
    Task SetRequiredDateAsync(DateTime requiredDate);
    Task SetPromisedDateAsync(DateTime promisedDate);
    Task SetWarehouseAsync(Guid warehouseId);
    Task SetDiscountAsync(decimal discountPercentage);
    Task SetShippingAsync(decimal shippingAmount, string currency);
    Task SetNotesAsync(string notes);
    Task SetTermsAsync(string terms);
    Task SetDropShipAsync(Guid customerId, string address);
    Task<Result> SubmitAsync();
    Task<Result> ApproveAsync(Guid approvedBy);
    Task<Result> SendAsync();
    Task<Result> AcknowledgeAsync();
    Task<Result> CancelAsync(Guid cancelledBy, string reason);
    Task<PurchaseOrderReceipt> CreateReceiptAsync(CreateReceiptRequest request);
    Task<PurchaseOrderSummary> GetSummaryAsync();
}

public class PurchaseOrderGrain : TenantGrainBase<PurchaseOrderState>, IPurchaseOrderGrain
{
    private readonly ILogger<PurchaseOrderGrain> _logger;

    public PurchaseOrderGrain(ILogger<PurchaseOrderGrain> logger)
    {
        _logger = logger;
    }

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
    {
        if (_state.State.PurchaseOrder != null)
            throw new InvalidOperationException("Purchase order already exists");

        var purchaseOrder = new PurchaseOrder(
            GetTenantId(),
            request.OrderNumber,
            request.OrderDate,
            request.SupplierId,
            request.Currency,
            request.BuyerId);

        if (request.RequiredDate.HasValue)
            purchaseOrder.SetRequiredDate(request.RequiredDate.Value);

        if (request.WarehouseId.HasValue)
            purchaseOrder.SetWarehouse(request.WarehouseId.Value);

        if (!string.IsNullOrEmpty(request.Notes))
            purchaseOrder.SetNotes(request.Notes);

        if (!string.IsNullOrEmpty(request.Terms))
            purchaseOrder.SetTerms(request.Terms);

        _state.State.PurchaseOrder = purchaseOrder;
        await SaveStateAsync();

        _logger.LogInformation("Purchase order {OrderNumber} created for tenant {TenantId}", 
            request.OrderNumber, GetTenantId());

        return purchaseOrder;
    }

    public Task<PurchaseOrder> GetPurchaseOrderAsync()
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        return Task.FromResult(_state.State.PurchaseOrder);
    }

    public async Task AddLineAsync(AddPurchaseOrderLineRequest request)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        var unitPrice = new Money(request.UnitPrice, request.Currency);
        _state.State.PurchaseOrder.AddLine(
            request.ProductId,
            request.ProductName,
            request.Quantity,
            request.UnitOfMeasure,
            unitPrice,
            request.RequiredDate,
            request.Notes);

        await SaveStateAsync();

        _logger.LogInformation("Line added to purchase order {OrderNumber} for tenant {TenantId}", 
            _state.State.PurchaseOrder.OrderNumber, GetTenantId());
    }

    public async Task UpdateLineAsync(int lineNumber, UpdatePurchaseOrderLineRequest request)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        var unitPrice = new Money(request.UnitPrice, request.Currency);
        _state.State.PurchaseOrder.UpdateLine(lineNumber, request.Quantity, unitPrice, request.RequiredDate);

        await SaveStateAsync();

        _logger.LogInformation("Line {LineNumber} updated in purchase order {OrderNumber} for tenant {TenantId}", 
            lineNumber, _state.State.PurchaseOrder.OrderNumber, GetTenantId());
    }

    public async Task RemoveLineAsync(int lineNumber)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.RemoveLine(lineNumber);
        await SaveStateAsync();

        _logger.LogInformation("Line {LineNumber} removed from purchase order {OrderNumber} for tenant {TenantId}", 
            lineNumber, _state.State.PurchaseOrder.OrderNumber, GetTenantId());
    }

    public async Task SetRequiredDateAsync(DateTime requiredDate)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetRequiredDate(requiredDate);
        await SaveStateAsync();
    }

    public async Task SetPromisedDateAsync(DateTime promisedDate)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetPromisedDate(promisedDate);
        await SaveStateAsync();
    }

    public async Task SetWarehouseAsync(Guid warehouseId)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetWarehouse(warehouseId);
        await SaveStateAsync();
    }

    public async Task SetDiscountAsync(decimal discountPercentage)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetDiscount(discountPercentage);
        await SaveStateAsync();
    }

    public async Task SetShippingAsync(decimal shippingAmount, string currency)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        var shipping = new Money(shippingAmount, currency);
        _state.State.PurchaseOrder.SetShipping(shipping);
        await SaveStateAsync();
    }

    public async Task SetNotesAsync(string notes)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetNotes(notes);
        await SaveStateAsync();
    }

    public async Task SetTermsAsync(string terms)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetTerms(terms);
        await SaveStateAsync();
    }

    public async Task SetDropShipAsync(Guid customerId, string address)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        _state.State.PurchaseOrder.SetDropShip(customerId, address);
        await SaveStateAsync();
    }

    public async Task<Result> SubmitAsync()
    {
        if (_state.State.PurchaseOrder == null)
            return Result.Failure("Purchase order not found");

        var result = _state.State.PurchaseOrder.Submit();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Purchase order {OrderNumber} submitted for tenant {TenantId}", 
                _state.State.PurchaseOrder.OrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> ApproveAsync(Guid approvedBy)
    {
        if (_state.State.PurchaseOrder == null)
            return Result.Failure("Purchase order not found");

        var result = _state.State.PurchaseOrder.Approve(approvedBy);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Purchase order {OrderNumber} approved by {ApprovedBy} for tenant {TenantId}", 
                _state.State.PurchaseOrder.OrderNumber, approvedBy, GetTenantId());
        }

        return result;
    }

    public async Task<Result> SendAsync()
    {
        if (_state.State.PurchaseOrder == null)
            return Result.Failure("Purchase order not found");

        var result = _state.State.PurchaseOrder.Send();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Purchase order {OrderNumber} sent for tenant {TenantId}", 
                _state.State.PurchaseOrder.OrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> AcknowledgeAsync()
    {
        if (_state.State.PurchaseOrder == null)
            return Result.Failure("Purchase order not found");

        var result = _state.State.PurchaseOrder.Acknowledge();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Purchase order {OrderNumber} acknowledged for tenant {TenantId}", 
                _state.State.PurchaseOrder.OrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> CancelAsync(Guid cancelledBy, string reason)
    {
        if (_state.State.PurchaseOrder == null)
            return Result.Failure("Purchase order not found");

        var result = _state.State.PurchaseOrder.Cancel(cancelledBy, reason);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Purchase order {OrderNumber} cancelled by {CancelledBy} for tenant {TenantId}: {Reason}", 
                _state.State.PurchaseOrder.OrderNumber, cancelledBy, GetTenantId(), reason);
        }

        return result;
    }

    public async Task<PurchaseOrderReceipt> CreateReceiptAsync(CreateReceiptRequest request)
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        var receipt = _state.State.PurchaseOrder.CreateReceipt(
            request.ReceiptNumber,
            request.ReceiptDate,
            request.ReceivedBy,
            request.ReceiptLines);

        await SaveStateAsync();

        _logger.LogInformation("Receipt {ReceiptNumber} created for purchase order {OrderNumber} for tenant {TenantId}", 
            request.ReceiptNumber, _state.State.PurchaseOrder.OrderNumber, GetTenantId());

        return receipt;
    }

    public Task<PurchaseOrderSummary> GetSummaryAsync()
    {
        if (_state.State.PurchaseOrder == null)
            throw new InvalidOperationException("Purchase order not found");

        var order = _state.State.PurchaseOrder;
        var summary = new PurchaseOrderSummary
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            SupplierId = order.SupplierId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            LineCount = order.Lines.Count,
            IsFullyReceived = order.IsFullyReceived(),
            IsPartiallyReceived = order.IsPartiallyReceived(),
            ReceiptPercentage = order.GetReceiptPercentage(),
            RequiredDate = order.RequiredDate,
            PromisedDate = order.PromisedDate,
            ApprovedBy = order.ApprovedBy,
            ApprovedAt = order.ApprovedAt
        };

        return Task.FromResult(summary);
    }
}

[GenerateSerializer]
public class PurchaseOrderState
{
    [Id(0)]
    public PurchaseOrder? PurchaseOrder { get; set; }
}

// Request DTOs
public record CreatePurchaseOrderRequest(
    string OrderNumber,
    DateTime OrderDate,
    Guid SupplierId,
    string Currency,
    Guid? BuyerId = null,
    DateTime? RequiredDate = null,
    Guid? WarehouseId = null,
    string? Notes = null,
    string? Terms = null);

public record AddPurchaseOrderLineRequest(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    string Currency,
    DateTime? RequiredDate = null,
    string? Notes = null);

public record UpdatePurchaseOrderLineRequest(
    decimal Quantity,
    decimal UnitPrice,
    string Currency,
    DateTime? RequiredDate = null);

public record CreateReceiptRequest(
    string ReceiptNumber,
    DateTime ReceiptDate,
    Guid ReceivedBy,
    List<ReceiptLineRequest> ReceiptLines);

public record PurchaseOrderSummary
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; }
    public DateTime OrderDate { get; init; }
    public Guid SupplierId { get; init; }
    public OrderStatus Status { get; init; }
    public Money TotalAmount { get; init; }
    public int LineCount { get; init; }
    public bool IsFullyReceived { get; init; }
    public bool IsPartiallyReceived { get; init; }
    public decimal ReceiptPercentage { get; init; }
    public DateTime? RequiredDate { get; init; }
    public DateTime? PromisedDate { get; init; }
    public Guid? ApprovedBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
}