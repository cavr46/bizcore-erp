using Orleans;
using BizCore.Orleans.Contracts.Sales;

namespace BizCore.ApiGateway.Services;

public interface ISalesService
{
    Task<CustomerState?> GetCustomerAsync(Guid customerId);
    Task<Result<Guid>> CreateCustomerAsync(CreateCustomerCommand command);
    Task<SalesOrderState?> GetSalesOrderAsync(Guid orderId);
    Task<Result<Guid>> CreateSalesOrderAsync(CreateSalesOrderCommand command);
}

public class SalesService : ISalesService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<SalesService> _logger;

    public SalesService(IClusterClient clusterClient, ILogger<SalesService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<CustomerState?> GetCustomerAsync(Guid customerId)
    {
        try
        {
            var customerGrain = _clusterClient.GetGrain<ICustomerGrain>(customerId);
            return await customerGrain.GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<Result<Guid>> CreateCustomerAsync(CreateCustomerCommand command)
    {
        try
        {
            var customerGrain = _clusterClient.GetGrain<ICustomerGrain>(Guid.NewGuid());
            return await customerGrain.CreateAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer {Name}", command.Name);
            return Result<Guid>.Failure($"Failed to create customer: {ex.Message}");
        }
    }

    public async Task<SalesOrderState?> GetSalesOrderAsync(Guid orderId)
    {
        try
        {
            var orderGrain = _clusterClient.GetGrain<ISalesOrderGrain>(orderId);
            return await orderGrain.GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<Result<Guid>> CreateSalesOrderAsync(CreateSalesOrderCommand command)
    {
        try
        {
            var orderGrain = _clusterClient.GetGrain<ISalesOrderGrain>(Guid.NewGuid());
            return await orderGrain.CreateAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales order for customer {CustomerId}", command.CustomerId);
            return Result<Guid>.Failure($"Failed to create sales order: {ex.Message}");
        }
    }
}