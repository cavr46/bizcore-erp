using BizCore.Inventory.Domain.Entities;
using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace BizCore.Inventory.Grains;

public interface IInventoryManagerGrain : IGrainWithGuidKey
{
    Task<string> GenerateSkuAsync(string prefix);
    Task<string> GenerateTransactionNumberAsync(TransactionType type);
    Task ProcessTransactionAsync(Guid transactionId);
    Task<InventorySummary> GetSummaryAsync();
    Task<List<LowStockAlert>> GetLowStockAlertsAsync();
    Task<List<ExpirationAlert>> GetExpirationAlertsAsync(int daysWarning);
    Task<bool> CheckStockAvailabilityAsync(Guid productId, Guid warehouseId, decimal requiredQuantity);
    Task<Result> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, string reference);
    Task<Result> ReleaseReservationAsync(Guid productId, Guid warehouseId, decimal quantity, string reference);
}

public class InventoryManagerGrain : TenantGrainBase<InventoryManagerState>, 
    IInventoryManagerGrain, IStreamSubscriptionObserver
{
    private IAsyncStream<InventoryTransactionProcessedEvent>? _transactionStream;
    private IAsyncStream<StockLevelChangedEvent>? _stockStream;

    public InventoryManagerGrain([PersistentState("inventoryManager", "Default")] IPersistentState<InventoryManagerState> state)
        : base(state)
    {
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        var streamProvider = this.GetStreamProvider("Default");
        
        _transactionStream = streamProvider.GetStream<InventoryTransactionProcessedEvent>(
            StreamId.Create("inventory-transactions", TenantId.ToString()));
        
        _stockStream = streamProvider.GetStream<StockLevelChangedEvent>(
            StreamId.Create("stock-levels", TenantId.ToString()));
        
        await _transactionStream.SubscribeAsync(this);
        await _stockStream.SubscribeAsync(this);
    }

    public async Task<string> GenerateSkuAsync(string prefix)
    {
        var nextNumber = _state.State.GetNextSkuNumber(prefix);
        _state.State.IncrementSkuNumber(prefix);
        await SaveStateAsync();
        
        return $"{prefix}{nextNumber:D6}";
    }

    public async Task<string> GenerateTransactionNumberAsync(TransactionType type)
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var nextNumber = _state.State.GetNextTransactionNumber(type, year, month);
        _state.State.IncrementTransactionNumber(type, year, month);
        await SaveStateAsync();
        
        return $"{type.Name.ToUpper()}{year}{month:D2}{nextNumber:D4}";
    }

    public async Task ProcessTransactionAsync(Guid transactionId)
    {
        var transactionGrain = GrainFactory.GetGrain<IInventoryTransactionGrain>($"{TenantId}_{transactionId}");
        var transaction = await transactionGrain.GetTransactionAsync();
        
        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        // Get or create stock level
        var stockLevelKey = GenerateStockLevelKey(
            transaction.ProductId, 
            transaction.ProductVariantId,
            transaction.WarehouseId, 
            transaction.LocationId,
            transaction.LotNumber,
            transaction.SerialNumber);
        
        var stockLevelGrain = GrainFactory.GetGrain<IStockLevelGrain>($"{TenantId}_{stockLevelKey}");
        var stockLevel = await stockLevelGrain.GetStockLevelAsync();
        
        if (stockLevel == null)
        {
            var createRequest = new CreateStockLevelRequest(
                TenantId,
                transaction.ProductId,
                transaction.ProductVariantId,
                transaction.WarehouseId,
                transaction.LocationId,
                transaction.UnitOfMeasure,
                transaction.LotNumber,
                transaction.SerialNumber,
                transaction.ExpirationDate);
            
            await stockLevelGrain.CreateStockLevelAsync(createRequest);
        }

        // Apply transaction to stock
        if (transaction.Type.IsInbound)
        {
            await stockLevelGrain.AddStockAsync(transaction.Quantity, transaction.UnitCost);
        }
        else
        {
            var result = await stockLevelGrain.RemoveStockAsync(transaction.Quantity);
            if (result.IsFailure)
                throw new InvalidOperationException($"Cannot process transaction: {result.Error}");
        }

        // Update manager statistics
        _state.State.TotalTransactionsProcessed++;
        _state.State.LastProcessedTransactionId = transactionId;
        _state.State.LastProcessedAt = DateTime.UtcNow;

        await SaveStateAsync();
    }

    public async Task<InventorySummary> GetSummaryAsync()
    {
        return new InventorySummary(
            _state.State.TotalTransactionsProcessed,
            _state.State.LastProcessedAt,
            _state.State.GetTotalProducts(),
            _state.State.GetTotalWarehouses(),
            _state.State.LowStockProducts.Count,
            _state.State.ExpiringProducts.Count);
    }

    public async Task<List<LowStockAlert>> GetLowStockAlertsAsync()
    {
        var alerts = new List<LowStockAlert>();
        
        foreach (var productData in _state.State.LowStockProducts.Values)
        {
            alerts.Add(new LowStockAlert(
                productData.ProductId,
                productData.WarehouseId,
                productData.CurrentStock,
                productData.ReorderPoint,
                productData.LastChecked));
        }
        
        return alerts;
    }

    public async Task<List<ExpirationAlert>> GetExpirationAlertsAsync(int daysWarning)
    {
        var alerts = new List<ExpirationAlert>();
        var cutoffDate = DateTime.UtcNow.AddDays(daysWarning);
        
        foreach (var productData in _state.State.ExpiringProducts.Values)
        {
            if (productData.ExpirationDate <= cutoffDate)
            {
                alerts.Add(new ExpirationAlert(
                    productData.ProductId,
                    productData.WarehouseId,
                    productData.LotNumber,
                    productData.ExpirationDate,
                    productData.Quantity));
            }
        }
        
        return alerts;
    }

    public async Task<bool> CheckStockAvailabilityAsync(Guid productId, Guid warehouseId, decimal requiredQuantity)
    {
        // Get all stock levels for this product/warehouse combination
        var stockLevelKey = GenerateStockLevelKey(productId, null, warehouseId, null, null, null);
        var stockLevelGrain = GrainFactory.GetGrain<IStockLevelGrain>($"{TenantId}_{stockLevelKey}");
        
        var stockLevel = await stockLevelGrain.GetStockLevelAsync();
        return stockLevel?.QuantityAvailable >= requiredQuantity;
    }

    public async Task<Result> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, string reference)
    {
        var stockLevelKey = GenerateStockLevelKey(productId, null, warehouseId, null, null, null);
        var stockLevelGrain = GrainFactory.GetGrain<IStockLevelGrain>($"{TenantId}_{stockLevelKey}");
        
        var result = await stockLevelGrain.ReserveStockAsync(quantity);
        if (result.IsSuccess)
        {
            _state.State.AddReservation(productId, warehouseId, quantity, reference);
            await SaveStateAsync();
        }
        
        return result;
    }

    public async Task<Result> ReleaseReservationAsync(Guid productId, Guid warehouseId, decimal quantity, string reference)
    {
        var stockLevelKey = GenerateStockLevelKey(productId, null, warehouseId, null, null, null);
        var stockLevelGrain = GrainFactory.GetGrain<IStockLevelGrain>($"{TenantId}_{stockLevelKey}");
        
        var result = await stockLevelGrain.ReleaseReservationAsync(quantity);
        if (result.IsSuccess)
        {
            _state.State.RemoveReservation(productId, warehouseId, quantity, reference);
            await SaveStateAsync();
        }
        
        return result;
    }

    public async Task OnNextAsync(InventoryTransactionProcessedEvent item, StreamSequenceToken? token = null)
    {
        await ProcessTransactionAsync(item.TransactionId);
    }

    public async Task OnNextAsync(StockLevelChangedEvent item, StreamSequenceToken? token = null)
    {
        // Update low stock alerts
        var productGrain = GrainFactory.GetGrain<IProductGrain>($"{TenantId}_{item.ProductId}");
        var product = await productGrain.GetProductAsync();
        
        if (product?.ReorderPoint.HasValue == true)
        {
            if (item.QuantityAvailable <= product.ReorderPoint.Value)
            {
                _state.State.AddLowStockProduct(item.ProductId, item.WarehouseId, item.QuantityAvailable, product.ReorderPoint.Value);
            }
            else
            {
                _state.State.RemoveLowStockProduct(item.ProductId, item.WarehouseId);
            }
        }
        
        await SaveStateAsync();
    }

    public async Task OnCompletedAsync()
    {
        // Stream completed
    }

    public async Task OnErrorAsync(Exception ex)
    {
        // Handle error
    }

    private static string GenerateStockLevelKey(
        Guid productId, 
        Guid? variantId, 
        Guid warehouseId, 
        Guid? locationId,
        string? lotNumber,
        string? serialNumber)
    {
        var key = $"{productId}_{warehouseId}";
        
        if (variantId.HasValue)
            key += $"_{variantId}";
        
        if (locationId.HasValue)
            key += $"_{locationId}";
        
        if (!string.IsNullOrEmpty(lotNumber))
            key += $"_{lotNumber}";
        
        if (!string.IsNullOrEmpty(serialNumber))
            key += $"_{serialNumber}";
        
        return key;
    }
}

public class InventoryManagerState
{
    public int TotalTransactionsProcessed { get; set; }
    public Guid? LastProcessedTransactionId { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public Dictionary<string, int> SkuCounters { get; set; } = new();
    public Dictionary<string, int> TransactionCounters { get; set; } = new();
    public Dictionary<string, ProductStockData> LowStockProducts { get; set; } = new();
    public Dictionary<string, ExpiringProductData> ExpiringProducts { get; set; } = new();
    public Dictionary<string, StockReservationData> Reservations { get; set; } = new();

    public int GetNextSkuNumber(string prefix)
    {
        if (!SkuCounters.TryGetValue(prefix, out var current))
        {
            current = 0;
        }
        return current + 1;
    }

    public void IncrementSkuNumber(string prefix)
    {
        if (SkuCounters.TryGetValue(prefix, out var current))
        {
            SkuCounters[prefix] = current + 1;
        }
        else
        {
            SkuCounters[prefix] = 1;
        }
    }

    public int GetNextTransactionNumber(TransactionType type, int year, int month)
    {
        var key = $"{type.Name}_{year}_{month}";
        if (!TransactionCounters.TryGetValue(key, out var current))
        {
            current = 0;
        }
        return current + 1;
    }

    public void IncrementTransactionNumber(TransactionType type, int year, int month)
    {
        var key = $"{type.Name}_{year}_{month}";
        if (TransactionCounters.TryGetValue(key, out var current))
        {
            TransactionCounters[key] = current + 1;
        }
        else
        {
            TransactionCounters[key] = 1;
        }
    }

    public void AddLowStockProduct(Guid productId, Guid warehouseId, decimal currentStock, decimal reorderPoint)
    {
        var key = $"{productId}_{warehouseId}";
        LowStockProducts[key] = new ProductStockData(productId, warehouseId, currentStock, reorderPoint, DateTime.UtcNow);
    }

    public void RemoveLowStockProduct(Guid productId, Guid warehouseId)
    {
        var key = $"{productId}_{warehouseId}";
        LowStockProducts.Remove(key);
    }

    public void AddExpiringProduct(Guid productId, Guid warehouseId, string lotNumber, DateTime expirationDate, decimal quantity)
    {
        var key = $"{productId}_{warehouseId}_{lotNumber}";
        ExpiringProducts[key] = new ExpiringProductData(productId, warehouseId, lotNumber, expirationDate, quantity);
    }

    public void AddReservation(Guid productId, Guid warehouseId, decimal quantity, string reference)
    {
        var key = $"{productId}_{warehouseId}_{reference}";
        Reservations[key] = new StockReservationData(productId, warehouseId, quantity, reference, DateTime.UtcNow);
    }

    public void RemoveReservation(Guid productId, Guid warehouseId, decimal quantity, string reference)
    {
        var key = $"{productId}_{warehouseId}_{reference}";
        Reservations.Remove(key);
    }

    public int GetTotalProducts()
    {
        return LowStockProducts.Select(p => p.Value.ProductId).Distinct().Count();
    }

    public int GetTotalWarehouses()
    {
        return LowStockProducts.Select(p => p.Value.WarehouseId).Distinct().Count();
    }
}

public record ProductStockData(
    Guid ProductId,
    Guid WarehouseId,
    decimal CurrentStock,
    decimal ReorderPoint,
    DateTime LastChecked);

public record ExpiringProductData(
    Guid ProductId,
    Guid WarehouseId,
    string LotNumber,
    DateTime ExpirationDate,
    decimal Quantity);

public record StockReservationData(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    string Reference,
    DateTime CreatedAt);

public record InventorySummary(
    int TotalTransactionsProcessed,
    DateTime? LastProcessedAt,
    int TotalProducts,
    int TotalWarehouses,
    int LowStockAlerts,
    int ExpirationAlerts);

public record LowStockAlert(
    Guid ProductId,
    Guid WarehouseId,
    decimal CurrentStock,
    decimal ReorderPoint,
    DateTime LastChecked);

public record ExpirationAlert(
    Guid ProductId,
    Guid WarehouseId,
    string LotNumber,
    DateTime ExpirationDate,
    decimal Quantity);

public record InventoryTransactionProcessedEvent(
    Guid TransactionId,
    Guid TenantId,
    TransactionType Type,
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity);