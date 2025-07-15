using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Purchasing.Domain.Entities;

public class PurchaseOrder : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string OrderNumber { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? RequiredDate { get; private set; }
    public DateTime? PromisedDate { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid? BuyerId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public Money SubTotal { get; private set; }
    public Money TaxAmount { get; private set; }
    public Money ShippingAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public Money DiscountAmount { get; private set; }
    public string? Notes { get; private set; }
    public string? Terms { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public bool IsDropShip { get; private set; }
    public Guid? DropShipCustomerId { get; private set; }
    public string? DropShipAddress { get; private set; }
    
    private readonly List<PurchaseOrderLine> _lines = new();
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();
    
    private readonly List<PurchaseOrderReceipt> _receipts = new();
    public IReadOnlyCollection<PurchaseOrderReceipt> Receipts => _receipts.AsReadOnly();

    private PurchaseOrder() { }

    public PurchaseOrder(
        Guid tenantId,
        string orderNumber,
        DateTime orderDate,
        Guid supplierId,
        string currency,
        Guid? buyerId = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        OrderNumber = orderNumber;
        OrderDate = orderDate;
        SupplierId = supplierId;
        BuyerId = buyerId;
        Currency = currency;
        Status = OrderStatus.Draft;
        ExchangeRate = 1.0m;
        SubTotal = Money.Zero(currency);
        TaxAmount = Money.Zero(currency);
        ShippingAmount = Money.Zero(currency);
        TotalAmount = Money.Zero(currency);
        DiscountAmount = Money.Zero(currency);
        
        AddDomainEvent(new PurchaseOrderCreatedDomainEvent(Id, TenantId, OrderNumber, SupplierId));
    }

    public void AddLine(
        Guid productId,
        string productName,
        decimal quantity,
        string unitOfMeasure,
        Money unitPrice,
        DateTime? requiredDate = null,
        string? notes = null)
    {
        if (Status != OrderStatus.Draft)
            throw new BusinessRuleValidationException("Cannot add lines to non-draft order");

        var lineNumber = _lines.Count + 1;
        var line = new PurchaseOrderLine(
            Id,
            lineNumber,
            productId,
            productName,
            quantity,
            unitOfMeasure,
            unitPrice,
            requiredDate,
            notes);
        
        _lines.Add(line);
        RecalculateTotals();
    }

    public void UpdateLine(int lineNumber, decimal quantity, Money unitPrice, DateTime? requiredDate = null)
    {
        if (Status != OrderStatus.Draft)
            throw new BusinessRuleValidationException("Cannot update lines in non-draft order");

        var line = _lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line != null)
        {
            line.UpdateQuantityAndPrice(quantity, unitPrice, requiredDate);
            RecalculateTotals();
        }
    }

    public void RemoveLine(int lineNumber)
    {
        if (Status != OrderStatus.Draft)
            throw new BusinessRuleValidationException("Cannot remove lines from non-draft order");

        var line = _lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line != null)
        {
            _lines.Remove(line);
            RenumberLines();
            RecalculateTotals();
        }
    }

    public void SetRequiredDate(DateTime requiredDate)
    {
        RequiredDate = requiredDate;
    }

    public void SetPromisedDate(DateTime promisedDate)
    {
        PromisedDate = promisedDate;
    }

    public void SetWarehouse(Guid warehouseId)
    {
        WarehouseId = warehouseId;
    }

    public void SetDiscount(decimal discountPercentage)
    {
        DiscountPercentage = discountPercentage;
        RecalculateTotals();
    }

    public void SetShipping(Money shippingAmount)
    {
        ShippingAmount = shippingAmount;
        RecalculateTotals();
    }

    public void SetNotes(string notes)
    {
        Notes = notes;
    }

    public void SetTerms(string terms)
    {
        Terms = terms;
    }

    public void SetDropShip(Guid customerId, string address)
    {
        IsDropShip = true;
        DropShipCustomerId = customerId;
        DropShipAddress = address;
    }

    public Result Submit()
    {
        if (Status != OrderStatus.Draft)
            return Result.Failure("Only draft orders can be submitted");

        if (!_lines.Any())
            return Result.Failure("Order must have at least one line");

        Status = OrderStatus.Submitted;
        AddDomainEvent(new PurchaseOrderSubmittedDomainEvent(Id, TenantId, SupplierId));
        
        return Result.Success();
    }

    public Result Approve(Guid approvedBy)
    {
        if (Status != OrderStatus.Submitted)
            return Result.Failure("Only submitted orders can be approved");

        Status = OrderStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        
        AddDomainEvent(new PurchaseOrderApprovedDomainEvent(Id, TenantId, SupplierId, approvedBy));
        
        return Result.Success();
    }

    public Result Send()
    {
        if (Status != OrderStatus.Approved)
            return Result.Failure("Only approved orders can be sent");

        Status = OrderStatus.Sent;
        AddDomainEvent(new PurchaseOrderSentDomainEvent(Id, TenantId, SupplierId));
        
        return Result.Success();
    }

    public Result Acknowledge()
    {
        if (Status != OrderStatus.Sent)
            return Result.Failure("Only sent orders can be acknowledged");

        Status = OrderStatus.Acknowledged;
        AddDomainEvent(new PurchaseOrderAcknowledgedDomainEvent(Id, TenantId, SupplierId));
        
        return Result.Success();
    }

    public Result Cancel(Guid cancelledBy, string reason)
    {
        if (Status == OrderStatus.Closed || Status == OrderStatus.Cancelled)
            return Result.Failure("Cannot cancel closed or already cancelled orders");

        Status = OrderStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        
        AddDomainEvent(new PurchaseOrderCancelledDomainEvent(Id, TenantId, SupplierId, cancelledBy, reason));
        
        return Result.Success();
    }

    public PurchaseOrderReceipt CreateReceipt(
        string receiptNumber,
        DateTime receiptDate,
        Guid receivedBy,
        List<ReceiptLineRequest> receiptLines)
    {
        var receipt = new PurchaseOrderReceipt(
            Id,
            receiptNumber,
            receiptDate,
            receivedBy);

        foreach (var receiptLine in receiptLines)
        {
            var orderLine = _lines.FirstOrDefault(l => l.Id == receiptLine.OrderLineId);
            if (orderLine != null)
            {
                receipt.AddLine(
                    receiptLine.OrderLineId,
                    orderLine.ProductId,
                    receiptLine.ReceivedQuantity,
                    orderLine.UnitOfMeasure,
                    receiptLine.QualityStatus,
                    receiptLine.Notes);
                
                orderLine.AddReceivedQuantity(receiptLine.ReceivedQuantity);
            }
        }

        _receipts.Add(receipt);
        
        // Check if order is fully received
        if (IsFullyReceived())
        {
            Status = OrderStatus.Closed;
            AddDomainEvent(new PurchaseOrderClosedDomainEvent(Id, TenantId, SupplierId));
        }

        AddDomainEvent(new PurchaseOrderReceiptCreatedDomainEvent(Id, TenantId, receipt.Id, receiptNumber));
        
        return receipt;
    }

    public bool IsFullyReceived()
    {
        return _lines.All(line => line.ReceivedQuantity >= line.OrderedQuantity);
    }

    public bool IsPartiallyReceived()
    {
        return _lines.Any(line => line.ReceivedQuantity > 0) && !IsFullyReceived();
    }

    public decimal GetReceiptPercentage()
    {
        if (!_lines.Any())
            return 0;

        var totalOrdered = _lines.Sum(l => l.OrderedQuantity);
        var totalReceived = _lines.Sum(l => l.ReceivedQuantity);
        
        return totalOrdered > 0 ? (totalReceived / totalOrdered) * 100 : 0;
    }

    private void RecalculateTotals()
    {
        SubTotal = _lines.Aggregate(Money.Zero(Currency), (sum, line) => sum.Add(line.LineTotal));
        DiscountAmount = SubTotal.Multiply(DiscountPercentage / 100);
        TaxAmount = SubTotal.Subtract(DiscountAmount).Multiply(0.1m); // 10% tax
        TotalAmount = SubTotal.Subtract(DiscountAmount).Add(TaxAmount).Add(ShippingAmount);
    }

    private void RenumberLines()
    {
        var orderedLines = _lines.OrderBy(l => l.LineNumber).ToList();
        for (int i = 0; i < orderedLines.Count; i++)
        {
            orderedLines[i].UpdateLineNumber(i + 1);
        }
    }
}

public class PurchaseOrderLine : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal { get; private set; }
    public DateTime? RequiredDate { get; private set; }
    public string? Notes { get; private set; }
    public LineStatus Status { get; private set; }

    private PurchaseOrderLine() { }

    public PurchaseOrderLine(
        Guid orderId,
        int lineNumber,
        Guid productId,
        string productName,
        decimal orderedQuantity,
        string unitOfMeasure,
        Money unitPrice,
        DateTime? requiredDate,
        string? notes)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        LineNumber = lineNumber;
        ProductId = productId;
        ProductName = productName;
        OrderedQuantity = orderedQuantity;
        ReceivedQuantity = 0;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        RequiredDate = requiredDate;
        Notes = notes;
        Status = LineStatus.Open;
        
        RecalculateLineTotal();
    }

    public void UpdateQuantityAndPrice(decimal quantity, Money unitPrice, DateTime? requiredDate = null)
    {
        OrderedQuantity = quantity;
        UnitPrice = unitPrice;
        RequiredDate = requiredDate;
        RecalculateLineTotal();
    }

    public void UpdateLineNumber(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public void AddReceivedQuantity(decimal quantity)
    {
        ReceivedQuantity += quantity;
        
        if (ReceivedQuantity >= OrderedQuantity)
        {
            Status = LineStatus.Closed;
        }
        else if (ReceivedQuantity > 0)
        {
            Status = LineStatus.PartiallyReceived;
        }
    }

    private void RecalculateLineTotal()
    {
        LineTotal = UnitPrice.Multiply(OrderedQuantity);
    }

    public decimal GetOutstandingQuantity()
    {
        return Math.Max(0, OrderedQuantity - ReceivedQuantity);
    }

    public decimal GetReceiptPercentage()
    {
        return OrderedQuantity > 0 ? (ReceivedQuantity / OrderedQuantity) * 100 : 0;
    }
}

public class PurchaseOrderReceipt : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public string ReceiptNumber { get; private set; }
    public DateTime ReceiptDate { get; private set; }
    public Guid ReceivedBy { get; private set; }
    public ReceiptStatus Status { get; private set; }
    public string? Notes { get; private set; }
    
    private readonly List<PurchaseOrderReceiptLine> _lines = new();
    public IReadOnlyCollection<PurchaseOrderReceiptLine> Lines => _lines.AsReadOnly();

    private PurchaseOrderReceipt() { }

    public PurchaseOrderReceipt(
        Guid orderId,
        string receiptNumber,
        DateTime receiptDate,
        Guid receivedBy)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ReceiptNumber = receiptNumber;
        ReceiptDate = receiptDate;
        ReceivedBy = receivedBy;
        Status = ReceiptStatus.Draft;
    }

    public void AddLine(
        Guid orderLineId,
        Guid productId,
        decimal receivedQuantity,
        string unitOfMeasure,
        QualityStatus qualityStatus,
        string? notes = null)
    {
        var lineNumber = _lines.Count + 1;
        var line = new PurchaseOrderReceiptLine(
            Id,
            lineNumber,
            orderLineId,
            productId,
            receivedQuantity,
            unitOfMeasure,
            qualityStatus,
            notes);
        
        _lines.Add(line);
    }

    public void Confirm()
    {
        Status = ReceiptStatus.Confirmed;
    }

    public void Reject(string reason)
    {
        Status = ReceiptStatus.Rejected;
        Notes = reason;
    }
}

public class PurchaseOrderReceiptLine : Entity<Guid>
{
    public Guid ReceiptId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public QualityStatus QualityStatus { get; private set; }
    public string? Notes { get; private set; }

    private PurchaseOrderReceiptLine() { }

    public PurchaseOrderReceiptLine(
        Guid receiptId,
        int lineNumber,
        Guid orderLineId,
        Guid productId,
        decimal receivedQuantity,
        string unitOfMeasure,
        QualityStatus qualityStatus,
        string? notes)
    {
        Id = Guid.NewGuid();
        ReceiptId = receiptId;
        LineNumber = lineNumber;
        OrderLineId = orderLineId;
        ProductId = productId;
        ReceivedQuantity = receivedQuantity;
        UnitOfMeasure = unitOfMeasure;
        QualityStatus = qualityStatus;
        Notes = notes;
    }
}

// Enums
public class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus Draft = new(1, nameof(Draft));
    public static readonly OrderStatus Submitted = new(2, nameof(Submitted));
    public static readonly OrderStatus Approved = new(3, nameof(Approved));
    public static readonly OrderStatus Sent = new(4, nameof(Sent));
    public static readonly OrderStatus Acknowledged = new(5, nameof(Acknowledged));
    public static readonly OrderStatus PartiallyReceived = new(6, nameof(PartiallyReceived));
    public static readonly OrderStatus Closed = new(7, nameof(Closed));
    public static readonly OrderStatus Cancelled = new(8, nameof(Cancelled));

    private OrderStatus(int value, string name) : base(name, value) { }
}

public class LineStatus : SmartEnum<LineStatus>
{
    public static readonly LineStatus Open = new(1, nameof(Open));
    public static readonly LineStatus PartiallyReceived = new(2, nameof(PartiallyReceived));
    public static readonly LineStatus Closed = new(3, nameof(Closed));
    public static readonly LineStatus Cancelled = new(4, nameof(Cancelled));

    private LineStatus(int value, string name) : base(name, value) { }
}

public class ReceiptStatus : SmartEnum<ReceiptStatus>
{
    public static readonly ReceiptStatus Draft = new(1, nameof(Draft));
    public static readonly ReceiptStatus Confirmed = new(2, nameof(Confirmed));
    public static readonly ReceiptStatus Rejected = new(3, nameof(Rejected));

    private ReceiptStatus(int value, string name) : base(name, value) { }
}

public class QualityStatus : SmartEnum<QualityStatus>
{
    public static readonly QualityStatus Accepted = new(1, nameof(Accepted));
    public static readonly QualityStatus Rejected = new(2, nameof(Rejected));
    public static readonly QualityStatus Quarantine = new(3, nameof(Quarantine));
    public static readonly QualityStatus Inspection = new(4, nameof(Inspection));

    private QualityStatus(int value, string name) : base(name, value) { }
}

// Request DTOs
public record ReceiptLineRequest(
    Guid OrderLineId,
    decimal ReceivedQuantity,
    QualityStatus QualityStatus,
    string? Notes);

// Domain Events
public record PurchaseOrderCreatedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    string OrderNumber,
    Guid SupplierId) : INotification;

public record PurchaseOrderSubmittedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid SupplierId) : INotification;

public record PurchaseOrderApprovedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid SupplierId,
    Guid ApprovedBy) : INotification;

public record PurchaseOrderSentDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid SupplierId) : INotification;

public record PurchaseOrderAcknowledgedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid SupplierId) : INotification;

public record PurchaseOrderCancelledDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid SupplierId,
    Guid CancelledBy,
    string Reason) : INotification;

public record PurchaseOrderClosedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid SupplierId) : INotification;

public record PurchaseOrderReceiptCreatedDomainEvent(
    Guid OrderId,
    Guid TenantId,
    Guid ReceiptId,
    string ReceiptNumber) : INotification;