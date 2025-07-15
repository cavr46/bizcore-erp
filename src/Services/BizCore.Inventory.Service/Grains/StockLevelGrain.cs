using BizCore.Inventory.Domain.Entities;
using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace BizCore.Inventory.Grains;

public interface IStockLevelGrain : IGrainWithStringKey
{
    Task<StockLevel?> GetStockLevelAsync();
    Task<StockLevel> CreateStockLevelAsync(CreateStockLevelRequest request);
    Task AddStockAsync(decimal quantity, Domain.Common.Money? unitCost);
    Task<Result> RemoveStockAsync(decimal quantity);
    Task<Result> ReserveStockAsync(decimal quantity);
    Task<Result> ReleaseReservationAsync(decimal quantity);
    Task AdjustStockAsync(decimal newQuantity, string reason);
    Task UpdateOnOrderQuantityAsync(decimal quantity);
    Task UpdateLastCountAsync();
    Task<bool> IsLowStockAsync(decimal reorderPoint);
    Task<bool> IsOverStockAsync(decimal maximumStock);
    Task<bool> IsExpiredAsync();
    Task<bool> IsExpiringSoonAsync(int daysWarning);
    Task<StockSummary> GetSummaryAsync();
}

public class StockLevelGrain : TenantGrainBase<StockLevelState>, IStockLevelGrain
{
    private IAsyncStream<StockLevelChangedEvent>? _stream;

    public StockLevelGrain([PersistentState("stockLevel", "Default")] IPersistentState<StockLevelState> state)
        : base(state)
    {
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        var streamProvider = this.GetStreamProvider("Default");
        _stream = streamProvider.GetStream<StockLevelChangedEvent>(
            StreamId.Create("stock-levels", TenantId.ToString()));
    }

    public async Task<StockLevel?> GetStockLevelAsync()
    {
        return _state.State.StockLevel;
    }

    public async Task<StockLevel> CreateStockLevelAsync(CreateStockLevelRequest request)
    {
        if (_state.State.StockLevel != null)
            throw new InvalidOperationException("Stock level already exists");

        var stockLevel = new StockLevel(
            request.TenantId,
            request.ProductId,
            request.WarehouseId,
            request.UnitOfMeasure);

        if (request.LocationId.HasValue)
            stockLevel.SetLocation(request.LocationId.Value);

        if (request.ProductVariantId.HasValue)
            stockLevel.SetVariant(request.ProductVariantId.Value);

        if (!string.IsNullOrEmpty(request.LotNumber))
            stockLevel.SetLotTracking(request.LotNumber, request.ExpirationDate);

        if (!string.IsNullOrEmpty(request.SerialNumber))
            stockLevel.SetSerialTracking(request.SerialNumber);

        _state.State.StockLevel = stockLevel;
        await SaveStateAsync();

        return stockLevel;
    }

    public async Task AddStockAsync(decimal quantity, Domain.Common.Money? unitCost)
    {
        if (_state.State.StockLevel == null)
            throw new InvalidOperationException("Stock level not found");

        _state.State.StockLevel.AddStock(quantity, unitCost);
        await SaveStateAsync();
        await PublishStockChangedEventAsync();
    }

    public async Task<Result> RemoveStockAsync(decimal quantity)
    {
        if (_state.State.StockLevel == null)
            return Result.Failure("Stock level not found");

        var result = _state.State.StockLevel.RemoveStock(quantity);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            await PublishStockChangedEventAsync();
        }

        return result;
    }

    public async Task<Result> ReserveStockAsync(decimal quantity)
    {
        if (_state.State.StockLevel == null)
            return Result.Failure("Stock level not found");

        var result = _state.State.StockLevel.ReserveStock(quantity);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            await PublishStockChangedEventAsync();
        }

        return result;
    }

    public async Task<Result> ReleaseReservationAsync(decimal quantity)
    {
        if (_state.State.StockLevel == null)
            return Result.Failure("Stock level not found");

        var result = _state.State.StockLevel.ReleaseReservation(quantity);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            await PublishStockChangedEventAsync();
        }

        return result;
    }

    public async Task AdjustStockAsync(decimal newQuantity, string reason)
    {
        if (_state.State.StockLevel == null)
            throw new InvalidOperationException("Stock level not found");

        _state.State.StockLevel.AdjustStock(newQuantity, reason);
        await SaveStateAsync();
        await PublishStockChangedEventAsync();
    }

    public async Task UpdateOnOrderQuantityAsync(decimal quantity)
    {
        if (_state.State.StockLevel == null)
            throw new InvalidOperationException("Stock level not found");

        _state.State.StockLevel.UpdateOnOrderQuantity(quantity);
        await SaveStateAsync();
    }

    public async Task UpdateLastCountAsync()
    {
        if (_state.State.StockLevel == null)
            throw new InvalidOperationException("Stock level not found");

        _state.State.StockLevel.UpdateLastCount();
        await SaveStateAsync();
    }

    public async Task<bool> IsLowStockAsync(decimal reorderPoint)
    {
        if (_state.State.StockLevel == null)
            return false;

        return _state.State.StockLevel.IsLowStock(reorderPoint);
    }

    public async Task<bool> IsOverStockAsync(decimal maximumStock)
    {
        if (_state.State.StockLevel == null)
            return false;

        return _state.State.StockLevel.IsOverStock(maximumStock);
    }

    public async Task<bool> IsExpiredAsync()
    {
        if (_state.State.StockLevel == null)
            return false;

        return _state.State.StockLevel.IsExpired();
    }

    public async Task<bool> IsExpiringSoonAsync(int daysWarning)
    {
        if (_state.State.StockLevel == null)
            return false;

        return _state.State.StockLevel.IsExpiringSoon(daysWarning);
    }

    public async Task<StockSummary> GetSummaryAsync()
    {
        if (_state.State.StockLevel == null)
            throw new InvalidOperationException("Stock level not found");

        var stock = _state.State.StockLevel;
        return new StockSummary(
            stock.ProductId,
            stock.ProductVariantId,
            stock.WarehouseId,
            stock.LocationId,
            stock.QuantityOnHand,
            stock.QuantityReserved,
            stock.QuantityAvailable,
            stock.QuantityOnOrder,
            stock.UnitOfMeasure,
            stock.AverageCost?.Amount,
            stock.LastCost?.Amount,
            stock.LastTransactionDate,
            stock.LotNumber,
            stock.SerialNumber,
            stock.ExpirationDate);
    }

    private async Task PublishStockChangedEventAsync()
    {
        if (_stream == null || _state.State.StockLevel == null)
            return;

        var stock = _state.State.StockLevel;
        var eventData = new StockLevelChangedEvent(
            stock.Id,
            TenantId,
            stock.ProductId,
            stock.ProductVariantId,
            stock.WarehouseId,
            stock.LocationId,
            stock.QuantityOnHand,
            stock.QuantityReserved,
            stock.QuantityAvailable,
            stock.QuantityOnOrder,
            stock.UnitOfMeasure,
            stock.AverageCost?.Amount,
            stock.LastCost?.Amount);

        await _stream.OnNextAsync(eventData);
    }
}

public class StockLevelState
{
    public StockLevel? StockLevel { get; set; }
}

public record CreateStockLevelRequest(
    Guid TenantId,
    Guid ProductId,
    Guid? ProductVariantId,
    Guid WarehouseId,
    Guid? LocationId,
    string UnitOfMeasure,
    string? LotNumber,
    string? SerialNumber,
    DateTime? ExpirationDate);

public record StockSummary(
    Guid ProductId,
    Guid? ProductVariantId,
    Guid WarehouseId,
    Guid? LocationId,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    decimal QuantityOnOrder,
    string UnitOfMeasure,
    decimal? AverageCost,
    decimal? LastCost,
    DateTime? LastTransactionDate,
    string? LotNumber,
    string? SerialNumber,
    DateTime? ExpirationDate);

public record StockLevelChangedEvent(
    Guid StockLevelId,
    Guid TenantId,
    Guid ProductId,
    Guid? ProductVariantId,
    Guid WarehouseId,
    Guid? LocationId,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    decimal QuantityOnOrder,
    string UnitOfMeasure,
    decimal? AverageCost,
    decimal? LastCost);