using Orleans;
using BizCore.Orleans.Contracts.Purchasing;

namespace BizCore.ApiGateway.Services;

public interface IPurchasingService
{
    Task<SupplierState?> GetSupplierAsync(Guid supplierId);
    Task<Result<Guid>> CreateSupplierAsync(CreateSupplierCommand command);
    Task<PurchaseOrderState?> GetPurchaseOrderAsync(Guid orderId);
    Task<Result<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderCommand command);
}

public class PurchasingService : IPurchasingService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PurchasingService> _logger;

    public PurchasingService(IClusterClient clusterClient, ILogger<PurchasingService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<SupplierState?> GetSupplierAsync(Guid supplierId)
    {
        try
        {
            var supplierGrain = _clusterClient.GetGrain<ISupplierGrain>(supplierId);
            return await supplierGrain.GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier {SupplierId}", supplierId);
            return null;
        }
    }

    public async Task<Result<Guid>> CreateSupplierAsync(CreateSupplierCommand command)
    {
        try
        {
            var supplierGrain = _clusterClient.GetGrain<ISupplierGrain>(Guid.NewGuid());
            return await supplierGrain.CreateAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier {Name}", command.Name);
            return Result<Guid>.Failure($"Failed to create supplier: {ex.Message}");
        }
    }

    public async Task<PurchaseOrderState?> GetPurchaseOrderAsync(Guid orderId)
    {
        try
        {
            var orderGrain = _clusterClient.GetGrain<IPurchaseOrderGrain>(orderId);
            return await orderGrain.GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<Result<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderCommand command)
    {
        try
        {
            var orderGrain = _clusterClient.GetGrain<IPurchaseOrderGrain>(Guid.NewGuid());
            return await orderGrain.CreateAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order for supplier {SupplierId}", command.SupplierId);
            return Result<Guid>.Failure($"Failed to create purchase order: {ex.Message}");
        }
    }
}