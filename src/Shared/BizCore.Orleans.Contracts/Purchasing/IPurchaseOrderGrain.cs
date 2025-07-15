using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Purchasing;

public interface IPurchaseOrderGrain : IEntityGrain<PurchaseOrderState>, ITenantGrain
{
    Task<Result<Guid>> CreateAsync(CreatePurchaseOrderCommand command);
    Task<Result> UpdateAsync(UpdatePurchaseOrderCommand command);
    Task<Result> AddLineAsync(PurchaseOrderLine line);
    Task<Result> UpdateLineAsync(int lineNumber, PurchaseOrderLine line);
    Task<Result> RemoveLineAsync(int lineNumber);
    Task<Result> SubmitForApprovalAsync(string submittedBy);
    Task<Result> ApproveAsync(string approvedBy);
    Task<Result> RejectAsync(string rejectedBy, string reason);
    Task<Result> SendToSupplierAsync(string sentBy);
    Task<Result> AcknowledgeAsync(DateTime expectedDeliveryDate);
    Task<Result> ReceiveAsync(ReceiveGoodsCommand command);
    Task<Result> CancelAsync(string cancelledBy, string reason);
    Task<Result<PurchaseOrderTotals>> CalculateTotalsAsync();
}

[GenerateSerializer]
public class PurchaseOrderState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string OrderNumber { get; set; } = string.Empty;
    [Id(3)] public DateTime OrderDate { get; set; }
    [Id(4)] public Guid SupplierId { get; set; }
    [Id(5)] public string SupplierName { get; set; } = string.Empty;
    [Id(6)] public PurchaseOrderStatus Status { get; set; }
    [Id(7)] public Address? DeliveryAddress { get; set; }
    [Id(8)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(9)] public DateTime? ExpectedDeliveryDate { get; set; }
    [Id(10)] public string? BuyerId { get; set; }
    [Id(11)] public string PaymentTerms { get; set; } = string.Empty;
    [Id(12)] public string CurrencyCode { get; set; } = "USD";
    [Id(13)] public List<PurchaseOrderLine> Lines { get; set; } = new();
    [Id(14)] public decimal SubTotal { get; set; }
    [Id(15)] public decimal TaxAmount { get; set; }
    [Id(16)] public decimal ShippingAmount { get; set; }
    [Id(17)] public decimal TotalAmount { get; set; }
    [Id(18)] public string? Notes { get; set; }
    [Id(19)] public DateTime CreatedAt { get; set; }
    [Id(20)] public string CreatedBy { get; set; } = string.Empty;
    [Id(21)] public DateTime? ApprovedAt { get; set; }
    [Id(22)] public string? ApprovedBy { get; set; }
    [Id(23)] public DateTime? SentAt { get; set; }
    [Id(24)] public string? SentBy { get; set; }
    [Id(25)] public DateTime? AcknowledgedAt { get; set; }
    [Id(26)] public List<GoodsReceipt> Receipts { get; set; } = new();
    [Id(27)] public Dictionary<string, string> CustomFields { get; set; } = new();
}

[GenerateSerializer]
public class PurchaseOrderLine
{
    [Id(0)] public int LineNumber { get; set; }
    [Id(1)] public Guid ProductId { get; set; }
    [Id(2)] public string ProductSKU { get; set; } = string.Empty;
    [Id(3)] public string ProductName { get; set; } = string.Empty;
    [Id(4)] public string? ProductDescription { get; set; }
    [Id(5)] public decimal Quantity { get; set; }
    [Id(6)] public string UnitOfMeasure { get; set; } = string.Empty;
    [Id(7)] public decimal UnitPrice { get; set; }
    [Id(8)] public decimal TaxRate { get; set; }
    [Id(9)] public decimal LineTotal { get; set; }
    [Id(10)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(11)] public decimal ReceivedQuantity { get; set; }
    [Id(12)] public string? Notes { get; set; }
    [Id(13)] public Dictionary<string, string> Specifications { get; set; } = new();
}

[GenerateSerializer]
public class GoodsReceipt
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public DateTime ReceiptDate { get; set; }
    [Id(2)] public string ReceiptNumber { get; set; } = string.Empty;
    [Id(3)] public string ReceivedBy { get; set; } = string.Empty;
    [Id(4)] public List<GoodsReceiptLine> Lines { get; set; } = new();
    [Id(5)] public string? Notes { get; set; }
    [Id(6)] public bool IsComplete { get; set; }
}

[GenerateSerializer]
public class GoodsReceiptLine
{
    [Id(0)] public int LineNumber { get; set; }
    [Id(1)] public decimal ReceivedQuantity { get; set; }
    [Id(2)] public decimal AcceptedQuantity { get; set; }
    [Id(3)] public decimal RejectedQuantity { get; set; }
    [Id(4)] public string? RejectionReason { get; set; }
    [Id(5)] public QualityStatus QualityStatus { get; set; }
    [Id(6)] public string? Notes { get; set; }
}

[GenerateSerializer]
public class CreatePurchaseOrderCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public Guid SupplierId { get; set; }
    [Id(2)] public DateTime OrderDate { get; set; }
    [Id(3)] public Address? DeliveryAddress { get; set; }
    [Id(4)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(5)] public string? BuyerId { get; set; }
    [Id(6)] public List<CreatePurchaseOrderLineCommand> Lines { get; set; } = new();
    [Id(7)] public string? Notes { get; set; }
    [Id(8)] public string CreatedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CreatePurchaseOrderLineCommand
{
    [Id(0)] public Guid ProductId { get; set; }
    [Id(1)] public decimal Quantity { get; set; }
    [Id(2)] public decimal? UnitPrice { get; set; }
    [Id(3)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(4)] public string? Notes { get; set; }
    [Id(5)] public Dictionary<string, string>? Specifications { get; set; }
}

[GenerateSerializer]
public class UpdatePurchaseOrderCommand
{
    [Id(0)] public Address? DeliveryAddress { get; set; }
    [Id(1)] public DateTime? RequestedDeliveryDate { get; set; }
    [Id(2)] public string? Notes { get; set; }
    [Id(3)] public string ModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ReceiveGoodsCommand
{
    [Id(0)] public DateTime ReceiptDate { get; set; }
    [Id(1)] public string ReceivedBy { get; set; } = string.Empty;
    [Id(2)] public List<ReceiveGoodsLineCommand> Lines { get; set; } = new();
    [Id(3)] public string? Notes { get; set; }
    [Id(4)] public bool IsComplete { get; set; }
}

[GenerateSerializer]
public class ReceiveGoodsLineCommand
{
    [Id(0)] public int LineNumber { get; set; }
    [Id(1)] public decimal ReceivedQuantity { get; set; }
    [Id(2)] public decimal AcceptedQuantity { get; set; }
    [Id(3)] public decimal RejectedQuantity { get; set; }
    [Id(4)] public string? RejectionReason { get; set; }
    [Id(5)] public QualityStatus QualityStatus { get; set; }
    [Id(6)] public string? Notes { get; set; }
}

[GenerateSerializer]
public class PurchaseOrderTotals
{
    [Id(0)] public decimal SubTotal { get; set; }
    [Id(1)] public decimal TaxAmount { get; set; }
    [Id(2)] public decimal ShippingAmount { get; set; }
    [Id(3)] public decimal TotalAmount { get; set; }
    [Id(4)] public Dictionary<decimal, decimal> TaxBreakdown { get; set; } = new();
    [Id(5)] public string CurrencyCode { get; set; } = "USD";
}

public enum PurchaseOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Sent = 3,
    Acknowledged = 4,
    InProgress = 5,
    PartiallyReceived = 6,
    Received = 7,
    Invoiced = 8,
    Completed = 9,
    Cancelled = 10,
    OnHold = 11
}

public enum QualityStatus
{
    Pending = 0,
    Passed = 1,
    Failed = 2,
    Conditional = 3,
    Rework = 4
}