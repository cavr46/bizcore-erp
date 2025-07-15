using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Inventory.Domain.Entities;

public class InventoryTransaction : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string TransactionNumber { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LocationId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public Money? UnitCost { get; private set; }
    public Money? TotalCost { get; private set; }
    public string? LotNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public Guid? RelatedTransactionId { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    private InventoryTransaction() { }

    public InventoryTransaction(
        Guid tenantId,
        string transactionNumber,
        DateTime transactionDate,
        TransactionType type,
        Guid productId,
        Guid warehouseId,
        decimal quantity,
        string unitOfMeasure)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        TransactionNumber = transactionNumber;
        TransactionDate = transactionDate;
        Type = type;
        ProductId = productId;
        WarehouseId = warehouseId;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        Status = TransactionStatus.Pending;
        
        AddDomainEvent(new InventoryTransactionCreatedDomainEvent(Id, TenantId, TransactionNumber, Type, ProductId));
    }

    public void SetLocation(Guid locationId)
    {
        LocationId = locationId;
    }

    public void SetVariant(Guid variantId)
    {
        ProductVariantId = variantId;
    }

    public void SetCost(Money unitCost)
    {
        UnitCost = unitCost;
        TotalCost = unitCost.Multiply(Quantity);
    }

    public void SetLotTracking(string lotNumber, DateTime? expirationDate)
    {
        LotNumber = lotNumber;
        ExpirationDate = expirationDate;
    }

    public void SetSerialTracking(string serialNumber)
    {
        SerialNumber = serialNumber;
    }

    public void SetReference(string reference, string? notes)
    {
        Reference = reference;
        Notes = notes;
    }

    public void SetRelatedTransaction(Guid relatedTransactionId)
    {
        RelatedTransactionId = relatedTransactionId;
    }

    public Result Approve(string approvedBy)
    {
        if (Status != TransactionStatus.Pending)
            return Result.Failure("Only pending transactions can be approved");

        Status = TransactionStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        
        AddDomainEvent(new InventoryTransactionApprovedDomainEvent(Id, TenantId, approvedBy));
        
        return Result.Success();
    }

    public Result Process()
    {
        if (Status != TransactionStatus.Approved)
            return Result.Failure("Only approved transactions can be processed");

        Status = TransactionStatus.Processed;
        
        AddDomainEvent(new InventoryTransactionProcessedDomainEvent(
            Id, 
            TenantId, 
            Type, 
            ProductId, 
            ProductVariantId,
            WarehouseId, 
            LocationId,
            Quantity, 
            UnitOfMeasure,
            LotNumber,
            SerialNumber,
            ExpirationDate));
        
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (Status == TransactionStatus.Processed)
            return Result.Failure("Processed transactions cannot be cancelled");

        Status = TransactionStatus.Cancelled;
        Notes = $"Cancelled: {reason}";
        
        AddDomainEvent(new InventoryTransactionCancelledDomainEvent(Id, TenantId, reason));
        
        return Result.Success();
    }

    public bool IsInbound()
    {
        return Type == TransactionType.Receipt ||
               Type == TransactionType.Adjustment ||
               Type == TransactionType.Transfer ||
               Type == TransactionType.Return;
    }

    public bool IsOutbound()
    {
        return Type == TransactionType.Issue ||
               Type == TransactionType.Sale ||
               Type == TransactionType.Waste ||
               Type == TransactionType.Transfer;
    }
}

public class TransactionType : SmartEnum<TransactionType>
{
    public static readonly TransactionType Receipt = new(1, nameof(Receipt), true);
    public static readonly TransactionType Issue = new(2, nameof(Issue), false);
    public static readonly TransactionType Adjustment = new(3, nameof(Adjustment), true);
    public static readonly TransactionType Transfer = new(4, nameof(Transfer), false);
    public static readonly TransactionType Sale = new(5, nameof(Sale), false);
    public static readonly TransactionType Return = new(6, nameof(Return), true);
    public static readonly TransactionType Waste = new(7, nameof(Waste), false);
    public static readonly TransactionType Production = new(8, nameof(Production), true);
    public static readonly TransactionType Consumption = new(9, nameof(Consumption), false);

    public bool IsInbound { get; }

    private TransactionType(int value, string name, bool isInbound) : base(name, value)
    {
        IsInbound = isInbound;
    }
}

public class TransactionStatus : SmartEnum<TransactionStatus>
{
    public static readonly TransactionStatus Pending = new(1, nameof(Pending));
    public static readonly TransactionStatus Approved = new(2, nameof(Approved));
    public static readonly TransactionStatus Processed = new(3, nameof(Processed));
    public static readonly TransactionStatus Cancelled = new(4, nameof(Cancelled));

    private TransactionStatus(int value, string name) : base(name, value) { }
}

// Domain Events
public record InventoryTransactionCreatedDomainEvent(
    Guid TransactionId,
    Guid TenantId,
    string TransactionNumber,
    TransactionType Type,
    Guid ProductId) : INotification;

public record InventoryTransactionApprovedDomainEvent(
    Guid TransactionId,
    Guid TenantId,
    string ApprovedBy) : INotification;

public record InventoryTransactionProcessedDomainEvent(
    Guid TransactionId,
    Guid TenantId,
    TransactionType Type,
    Guid ProductId,
    Guid? ProductVariantId,
    Guid WarehouseId,
    Guid? LocationId,
    decimal Quantity,
    string UnitOfMeasure,
    string? LotNumber,
    string? SerialNumber,
    DateTime? ExpirationDate) : INotification;

public record InventoryTransactionCancelledDomainEvent(
    Guid TransactionId,
    Guid TenantId,
    string Reason) : INotification;