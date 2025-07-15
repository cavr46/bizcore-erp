using BizCore.Inventory.Domain.Entities;
using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace BizCore.Inventory.Grains;

public interface IInventoryTransactionGrain : IGrainWithStringKey
{
    Task<InventoryTransaction?> GetTransactionAsync();
    Task<InventoryTransaction> CreateTransactionAsync(CreateTransactionRequest request);
    Task SetLocationAsync(Guid locationId);
    Task SetVariantAsync(Guid variantId);
    Task SetCostAsync(Domain.Common.Money unitCost);
    Task SetLotTrackingAsync(string lotNumber, DateTime? expirationDate);
    Task SetSerialTrackingAsync(string serialNumber);
    Task SetReferenceAsync(string reference, string? notes);
    Task SetRelatedTransactionAsync(Guid relatedTransactionId);
    Task<Result> ApproveAsync(string approvedBy);
    Task<Result> ProcessAsync();
    Task<Result> CancelAsync(string reason);
}

public class InventoryTransactionGrain : TenantGrainBase<InventoryTransactionState>, IInventoryTransactionGrain
{
    private IAsyncStream<InventoryTransactionProcessedEvent>? _stream;

    public InventoryTransactionGrain([PersistentState("inventoryTransaction", "Default")] IPersistentState<InventoryTransactionState> state)
        : base(state)
    {
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        var streamProvider = this.GetStreamProvider("Default");
        _stream = streamProvider.GetStream<InventoryTransactionProcessedEvent>(
            StreamId.Create("inventory-transactions", TenantId.ToString()));
    }

    public async Task<InventoryTransaction?> GetTransactionAsync()
    {
        return _state.State.Transaction;
    }

    public async Task<InventoryTransaction> CreateTransactionAsync(CreateTransactionRequest request)
    {
        if (_state.State.Transaction != null)
            throw new InvalidOperationException("Transaction already exists");

        var transaction = new InventoryTransaction(
            request.TenantId,
            request.TransactionNumber,
            request.TransactionDate,
            request.Type,
            request.ProductId,
            request.WarehouseId,
            request.Quantity,
            request.UnitOfMeasure);

        _state.State.Transaction = transaction;
        await SaveStateAsync();

        return transaction;
    }

    public async Task SetLocationAsync(Guid locationId)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetLocation(locationId);
        await SaveStateAsync();
    }

    public async Task SetVariantAsync(Guid variantId)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetVariant(variantId);
        await SaveStateAsync();
    }

    public async Task SetCostAsync(Domain.Common.Money unitCost)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetCost(unitCost);
        await SaveStateAsync();
    }

    public async Task SetLotTrackingAsync(string lotNumber, DateTime? expirationDate)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetLotTracking(lotNumber, expirationDate);
        await SaveStateAsync();
    }

    public async Task SetSerialTrackingAsync(string serialNumber)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetSerialTracking(serialNumber);
        await SaveStateAsync();
    }

    public async Task SetReferenceAsync(string reference, string? notes)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetReference(reference, notes);
        await SaveStateAsync();
    }

    public async Task SetRelatedTransactionAsync(Guid relatedTransactionId)
    {
        if (_state.State.Transaction == null)
            throw new InvalidOperationException("Transaction not found");

        _state.State.Transaction.SetRelatedTransaction(relatedTransactionId);
        await SaveStateAsync();
    }

    public async Task<Result> ApproveAsync(string approvedBy)
    {
        if (_state.State.Transaction == null)
            return Result.Failure("Transaction not found");

        var result = _state.State.Transaction.Approve(approvedBy);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
        }

        return result;
    }

    public async Task<Result> ProcessAsync()
    {
        if (_state.State.Transaction == null)
            return Result.Failure("Transaction not found");

        var result = _state.State.Transaction.Process();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            await PublishProcessedEventAsync();
        }

        return result;
    }

    public async Task<Result> CancelAsync(string reason)
    {
        if (_state.State.Transaction == null)
            return Result.Failure("Transaction not found");

        var result = _state.State.Transaction.Cancel(reason);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
        }

        return result;
    }

    private async Task PublishProcessedEventAsync()
    {
        if (_stream == null || _state.State.Transaction == null)
            return;

        var transaction = _state.State.Transaction;
        var eventData = new InventoryTransactionProcessedEvent(
            transaction.Id,
            TenantId,
            transaction.Type,
            transaction.ProductId,
            transaction.WarehouseId,
            transaction.Quantity);

        await _stream.OnNextAsync(eventData);
    }
}

public class InventoryTransactionState
{
    public InventoryTransaction? Transaction { get; set; }
}

public record CreateTransactionRequest(
    Guid TenantId,
    string TransactionNumber,
    DateTime TransactionDate,
    TransactionType Type,
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    string UnitOfMeasure);