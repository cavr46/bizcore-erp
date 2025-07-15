using Orleans;
using BizCore.Orleans.Contracts.Inventory;

namespace BizCore.ApiGateway.Services;

public interface IInventoryService
{
    Task<ProductState?> GetProductAsync(Guid productId);
    Task<Result<Guid>> CreateProductAsync(CreateProductCommand command);
    Task<Result<StockInfo>> GetStockInfoAsync(Guid productId);
    Task<Result> AdjustStockAsync(Guid productId, StockAdjustmentCommand command);
}

public class InventoryService : IInventoryService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IClusterClient clusterClient, ILogger<InventoryService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<ProductState?> GetProductAsync(Guid productId)
    {
        try
        {
            var productGrain = _clusterClient.GetGrain<IProductGrain>(productId);
            return await productGrain.GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            return null;
        }
    }

    public async Task<Result<Guid>> CreateProductAsync(CreateProductCommand command)
    {
        try
        {
            var productGrain = _clusterClient.GetGrain<IProductGrain>(Guid.NewGuid());
            return await productGrain.CreateAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product {SKU}", command.SKU);
            return Result<Guid>.Failure($"Failed to create product: {ex.Message}");
        }
    }

    public async Task<Result<StockInfo>> GetStockInfoAsync(Guid productId)
    {
        try
        {
            var productGrain = _clusterClient.GetGrain<IProductGrain>(productId);
            return await productGrain.GetStockInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock info for product {ProductId}", productId);
            return Result<StockInfo>.Failure($"Failed to get stock info: {ex.Message}");
        }
    }

    public async Task<Result> AdjustStockAsync(Guid productId, StockAdjustmentCommand command)
    {
        try
        {
            var productGrain = _clusterClient.GetGrain<IProductGrain>(productId);
            return await productGrain.AdjustStockAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for product {ProductId}", productId);
            return Result.Failure($"Failed to adjust stock: {ex.Message}");
        }
    }
}