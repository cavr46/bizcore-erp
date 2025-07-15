using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Sales;

public interface ISalesOrderGrain : IEntityGrain<SalesOrderState>, ITenantGrain
{
    Task<Result<Guid>> CreateAsync(CreateSalesOrderCommand command);
    Task<Result> UpdateAsync(UpdateSalesOrderCommand command);
    Task<Result> AddLineAsync(SalesOrderLine line);
    Task<Result> UpdateLineAsync(int lineNumber, SalesOrderLine line);
    Task<Result> RemoveLineAsync(int lineNumber);
    Task<Result> SubmitForApprovalAsync(string submittedBy);
    Task<Result> ApproveAsync(string approvedBy);
    Task<Result> RejectAsync(string rejectedBy, string reason);
    Task<Result> ConvertToInvoiceAsync(string convertedBy);
    Task<Result> CancelAsync(string cancelledBy, string reason);
    Task<Result<SalesOrderTotals>> CalculateTotalsAsync();
}

[GenerateSerializer]
public class SalesOrderState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string OrderNumber { get; set; } = string.Empty;
    [Id(3)] public DateTime OrderDate { get; set; }
    [Id(4)] public Guid CustomerId { get; set; }
    [Id(5)] public string CustomerName { get; set; } = string.Empty;
    [Id(6)] public string CustomerPO { get; set; } = string.Empty;
    [Id(7)] public SalesOrderStatus Status { get; set; }
    [Id(8)] public Address? ShippingAddress { get; set; }
    [Id(9)] public Address? BillingAddress { get; set; }
    [Id(10)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(11)] public string PaymentTerms { get; set; } = string.Empty;
    [Id(12)] public string? SalespersonId { get; set; }
    [Id(13)] public decimal DiscountPercentage { get; set; }
    [Id(14)] public List<SalesOrderLine> Lines { get; set; } = new();
    [Id(15)] public decimal SubTotal { get; set; }
    [Id(16)] public decimal TaxAmount { get; set; }
    [Id(17)] public decimal ShippingAmount { get; set; }
    [Id(18)] public decimal TotalAmount { get; set; }
    [Id(19)] public string? Notes { get; set; }
    [Id(20)] public DateTime CreatedAt { get; set; }
    [Id(21)] public string CreatedBy { get; set; } = string.Empty;
    [Id(22)] public DateTime? ApprovedAt { get; set; }
    [Id(23)] public string? ApprovedBy { get; set; }
    [Id(24)] public Guid? InvoiceId { get; set; }
}

[GenerateSerializer]
public class SalesOrderLine
{
    [Id(0)] public int LineNumber { get; set; }
    [Id(1)] public Guid ProductId { get; set; }
    [Id(2)] public string ProductSKU { get; set; } = string.Empty;
    [Id(3)] public string ProductName { get; set; } = string.Empty;
    [Id(4)] public decimal Quantity { get; set; }
    [Id(5)] public string UnitOfMeasure { get; set; } = string.Empty;
    [Id(6)] public decimal UnitPrice { get; set; }
    [Id(7)] public decimal DiscountPercentage { get; set; }
    [Id(8)] public decimal TaxRate { get; set; }
    [Id(9)] public decimal LineTotal { get; set; }
    [Id(10)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(11)] public Guid? WarehouseId { get; set; }
    [Id(12)] public string? Notes { get; set; }
}

[GenerateSerializer]
public class CreateSalesOrderCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public Guid CustomerId { get; set; }
    [Id(2)] public string CustomerPO { get; set; } = string.Empty;
    [Id(3)] public DateTime OrderDate { get; set; }
    [Id(4)] public Address? ShippingAddress { get; set; }
    [Id(5)] public Address? BillingAddress { get; set; }
    [Id(6)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(7)] public string? SalespersonId { get; set; }
    [Id(8)] public List<CreateSalesOrderLineCommand> Lines { get; set; } = new();
    [Id(9)] public string? Notes { get; set; }
    [Id(10)] public string CreatedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CreateSalesOrderLineCommand
{
    [Id(0)] public Guid ProductId { get; set; }
    [Id(1)] public decimal Quantity { get; set; }
    [Id(2)] public decimal? UnitPrice { get; set; }
    [Id(3)] public decimal? DiscountPercentage { get; set; }
    [Id(4)] public Guid? WarehouseId { get; set; }
    [Id(5)] public string? Notes { get; set; }
}

[GenerateSerializer]
public class UpdateSalesOrderCommand
{
    [Id(0)] public string? CustomerPO { get; set; }
    [Id(1)] public Address? ShippingAddress { get; set; }
    [Id(2)] public Address? BillingAddress { get; set; }
    [Id(3)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(4)] public string? Notes { get; set; }
    [Id(5)] public string ModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SalesOrderTotals
{
    [Id(0)] public decimal SubTotal { get; set; }
    [Id(1)] public decimal DiscountAmount { get; set; }
    [Id(2)] public decimal TaxableAmount { get; set; }
    [Id(3)] public decimal TaxAmount { get; set; }
    [Id(4)] public decimal ShippingAmount { get; set; }
    [Id(5)] public decimal TotalAmount { get; set; }
    [Id(6)] public Dictionary<decimal, decimal> TaxBreakdown { get; set; } = new();
}

public enum SalesOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    InProgress = 3,
    PartiallyShipped = 4,
    Shipped = 5,
    Invoiced = 6,
    Completed = 7,
    Cancelled = 8,
    OnHold = 9
}