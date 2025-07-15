using BizCore.Domain.Common;

namespace BizCore.Inventory.Domain.Entities;

public class StockLevel : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LocationId { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public decimal QuantityAvailable { get; private set; }
    public decimal QuantityOnOrder { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public Money? AverageCost { get; private set; }
    public Money? LastCost { get; private set; }
    public DateTime? LastTransactionDate { get; private set; }
    public DateTime? LastCountDate { get; private set; }
    public string? LotNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateTime? ExpirationDate { get; private set; }

    private StockLevel() { }

    public StockLevel(
        Guid tenantId,
        Guid productId,
        Guid warehouseId,
        string unitOfMeasure)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        ProductId = productId;
        WarehouseId = warehouseId;
        UnitOfMeasure = unitOfMeasure;
        QuantityOnHand = 0;
        QuantityReserved = 0;
        QuantityAvailable = 0;
        QuantityOnOrder = 0;
        
        RecalculateAvailableQuantity();
    }

    public void SetLocation(Guid locationId)
    {
        LocationId = locationId;
    }

    public void SetVariant(Guid variantId)
    {
        ProductVariantId = variantId;
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

    public void AddStock(decimal quantity, Money? unitCost)
    {
        if (quantity <= 0)
            throw new BusinessRuleValidationException("Quantity must be positive");

        QuantityOnHand += quantity;
        LastTransactionDate = DateTime.UtcNow;
        
        if (unitCost != null)
        {
            UpdateAverageCost(quantity, unitCost);
            LastCost = unitCost;
        }
        
        RecalculateAvailableQuantity();
        
        AddDomainEvent(new StockLevelChangedDomainEvent(
            Id, TenantId, ProductId, WarehouseId, QuantityOnHand, QuantityAvailable));
    }

    public Result RemoveStock(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be positive");

        if (quantity > QuantityAvailable)
            return Result.Failure($"Insufficient stock. Available: {QuantityAvailable}, Requested: {quantity}");

        QuantityOnHand -= quantity;
        LastTransactionDate = DateTime.UtcNow;
        
        RecalculateAvailableQuantity();
        
        AddDomainEvent(new StockLevelChangedDomainEvent(
            Id, TenantId, ProductId, WarehouseId, QuantityOnHand, QuantityAvailable));
        
        return Result.Success();
    }

    public Result ReserveStock(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be positive");

        if (quantity > QuantityAvailable)
            return Result.Failure($"Insufficient available stock. Available: {QuantityAvailable}, Requested: {quantity}");

        QuantityReserved += quantity;
        RecalculateAvailableQuantity();
        
        AddDomainEvent(new StockReservedDomainEvent(
            Id, TenantId, ProductId, WarehouseId, quantity, QuantityReserved));
        
        return Result.Success();
    }

    public Result ReleaseReservation(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be positive");

        if (quantity > QuantityReserved)
            return Result.Failure($"Cannot release more than reserved. Reserved: {QuantityReserved}, Requested: {quantity}");

        QuantityReserved -= quantity;
        RecalculateAvailableQuantity();
        
        AddDomainEvent(new StockReservationReleasedDomainEvent(
            Id, TenantId, ProductId, WarehouseId, quantity, QuantityReserved));
        
        return Result.Success();
    }

    public void UpdateOnOrderQuantity(decimal quantity)
    {
        QuantityOnOrder = quantity;
    }

    public void AdjustStock(decimal newQuantity, string reason)
    {
        var difference = newQuantity - QuantityOnHand;
        QuantityOnHand = newQuantity;
        LastTransactionDate = DateTime.UtcNow;
        
        RecalculateAvailableQuantity();
        
        AddDomainEvent(new StockAdjustedDomainEvent(
            Id, TenantId, ProductId, WarehouseId, difference, newQuantity, reason));
    }

    public void UpdateLastCount()
    {
        LastCountDate = DateTime.UtcNow;
    }

    public bool IsExpired()
    {
        return ExpirationDate.HasValue && ExpirationDate.Value <= DateTime.UtcNow;
    }

    public bool IsExpiringSoon(int daysWarning)
    {
        return ExpirationDate.HasValue && 
               ExpirationDate.Value <= DateTime.UtcNow.AddDays(daysWarning);
    }

    public bool IsLowStock(decimal reorderPoint)
    {
        return QuantityAvailable <= reorderPoint;
    }

    public bool IsOverStock(decimal maximumStock)
    {
        return QuantityOnHand >= maximumStock;
    }

    private void RecalculateAvailableQuantity()
    {
        QuantityAvailable = QuantityOnHand - QuantityReserved;
    }

    private void UpdateAverageCost(decimal newQuantity, Money newCost)
    {
        if (AverageCost == null)
        {
            AverageCost = newCost;
            return;
        }

        var totalValue = AverageCost.Multiply(QuantityOnHand).Add(newCost.Multiply(newQuantity));
        var totalQuantity = QuantityOnHand + newQuantity;
        
        if (totalQuantity > 0)
        {
            AverageCost = totalValue.Divide(totalQuantity);
        }
    }
}

public record StockLevelChangedDomainEvent(
    Guid StockLevelId,
    Guid TenantId,
    Guid ProductId,
    Guid WarehouseId,
    decimal QuantityOnHand,
    decimal QuantityAvailable) : INotification;

public record StockReservedDomainEvent(
    Guid StockLevelId,
    Guid TenantId,
    Guid ProductId,
    Guid WarehouseId,
    decimal ReservedQuantity,
    decimal TotalReserved) : INotification;

public record StockReservationReleasedDomainEvent(
    Guid StockLevelId,
    Guid TenantId,
    Guid ProductId,
    Guid WarehouseId,
    decimal ReleasedQuantity,
    decimal TotalReserved) : INotification;

public record StockAdjustedDomainEvent(
    Guid StockLevelId,
    Guid TenantId,
    Guid ProductId,
    Guid WarehouseId,
    decimal AdjustmentQuantity,
    decimal NewQuantity,
    string Reason) : INotification;