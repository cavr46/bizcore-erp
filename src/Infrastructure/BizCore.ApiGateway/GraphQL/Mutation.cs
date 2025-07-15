using BizCore.ApiGateway.Services;
using BizCore.Orleans.Contracts.Accounting;
using BizCore.Orleans.Contracts.Inventory;
using BizCore.Orleans.Contracts.Sales;
using BizCore.Orleans.Contracts.Purchasing;
using HotChocolate.Authorization;

namespace BizCore.ApiGateway.GraphQL;

[Authorize]
public class Mutation
{
    // Accounting Mutations
    public async Task<Result<Guid>> CreateAccount(
        AccountInitCommand command,
        [Service] IAccountingService accountingService)
    {
        return await accountingService.CreateAccountAsync(command);
    }

    public async Task<Result<JournalEntryState>> CreateJournalEntry(
        CreateJournalEntryCommand command,
        [Service] IAccountingService accountingService)
    {
        return await accountingService.CreateJournalEntryAsync(command);
    }

    // Inventory Mutations
    public async Task<Result<Guid>> CreateProduct(
        CreateProductCommand command,
        [Service] IInventoryService inventoryService)
    {
        return await inventoryService.CreateProductAsync(command);
    }

    public async Task<Result> AdjustStock(
        Guid productId,
        StockAdjustmentCommand command,
        [Service] IInventoryService inventoryService)
    {
        return await inventoryService.AdjustStockAsync(productId, command);
    }

    // Sales Mutations
    public async Task<Result<Guid>> CreateCustomer(
        CreateCustomerCommand command,
        [Service] ISalesService salesService)
    {
        return await salesService.CreateCustomerAsync(command);
    }

    public async Task<Result<Guid>> CreateSalesOrder(
        CreateSalesOrderCommand command,
        [Service] ISalesService salesService)
    {
        return await salesService.CreateSalesOrderAsync(command);
    }

    // Purchasing Mutations
    public async Task<Result<Guid>> CreateSupplier(
        CreateSupplierCommand command,
        [Service] IPurchasingService purchasingService)
    {
        return await purchasingService.CreateSupplierAsync(command);
    }

    public async Task<Result<Guid>> CreatePurchaseOrder(
        CreatePurchaseOrderCommand command,
        [Service] IPurchasingService purchasingService)
    {
        return await purchasingService.CreatePurchaseOrderAsync(command);
    }
}