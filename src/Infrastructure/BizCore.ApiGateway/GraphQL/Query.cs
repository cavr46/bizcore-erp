using BizCore.ApiGateway.Services;
using BizCore.Orleans.Contracts.Accounting;
using BizCore.Orleans.Contracts.Inventory;
using BizCore.Orleans.Contracts.Sales;
using BizCore.Orleans.Contracts.Purchasing;
using HotChocolate.Authorization;

namespace BizCore.ApiGateway.GraphQL;

[Authorize]
public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<List<TenantInfo>> GetTenants([Service] ITenantService tenantService)
    {
        return await tenantService.GetTenantsAsync();
    }

    // Accounting Queries
    public async Task<AccountState?> GetAccount(
        Guid accountId,
        [Service] IAccountingService accountingService)
    {
        return await accountingService.GetAccountAsync(accountId);
    }

    public async Task<Result<AccountMovementsState>> GetAccountMovements(
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        [Service] IAccountingService accountingService)
    {
        return await accountingService.GetAccountMovementsAsync(accountId, startDate, endDate);
    }

    // Inventory Queries
    public async Task<ProductState?> GetProduct(
        Guid productId,
        [Service] IInventoryService inventoryService)
    {
        return await inventoryService.GetProductAsync(productId);
    }

    public async Task<Result<StockInfo>> GetStockInfo(
        Guid productId,
        [Service] IInventoryService inventoryService)
    {
        return await inventoryService.GetStockInfoAsync(productId);
    }

    // Sales Queries
    public async Task<CustomerState?> GetCustomer(
        Guid customerId,
        [Service] ISalesService salesService)
    {
        return await salesService.GetCustomerAsync(customerId);
    }

    public async Task<SalesOrderState?> GetSalesOrder(
        Guid orderId,
        [Service] ISalesService salesService)
    {
        return await salesService.GetSalesOrderAsync(orderId);
    }

    // Purchasing Queries
    public async Task<SupplierState?> GetSupplier(
        Guid supplierId,
        [Service] IPurchasingService purchasingService)
    {
        return await purchasingService.GetSupplierAsync(supplierId);
    }

    public async Task<PurchaseOrderState?> GetPurchaseOrder(
        Guid orderId,
        [Service] IPurchasingService purchasingService)
    {
        return await purchasingService.GetPurchaseOrderAsync(orderId);
    }
}