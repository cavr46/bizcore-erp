using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Inventory;
using BizCore.Orleans.Core.Base;

namespace BizCore.Inventory.Service.Grains;

public class ProductGrain : TenantGrainBase<ProductState>, IProductGrain
{
    public ProductGrain(
        [PersistentState("product", "InventoryStore")] IPersistentState<ProductState> state)
        : base(state)
    {
    }

    public async Task<Result<Guid>> CreateAsync(CreateProductCommand command)
    {
        if (State.Id != Guid.Empty)
            return Result<Guid>.Failure("Product already exists");

        if (string.IsNullOrWhiteSpace(command.SKU))
            return Result<Guid>.Failure("SKU is required");

        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Guid>.Failure("Product name is required");

        State.Id = this.GetPrimaryKey();
        State.TenantId = command.TenantId;
        State.SKU = command.SKU;
        State.Name = command.Name;
        State.Description = command.Description;
        State.Type = command.Type;
        State.CategoryCode = command.CategoryCode;
        State.UnitOfMeasure = command.UnitOfMeasure;
        State.StandardCost = command.StandardCost;
        State.ListPrice = command.ListPrice;
        State.IsActive = true;
        State.TrackInventory = command.TrackInventory;
        State.AllowBackorders = false;
        State.MinimumStock = command.MinimumStock;
        State.ReorderPoint = command.ReorderPoint;
        State.CreatedAt = DateTime.UtcNow;
        State.Variants = new List<ProductVariant>();
        State.WarehouseStock = new Dictionary<string, decimal>();
        State.ReservedStock = new Dictionary<string, decimal>();
        State.Attributes = new Dictionary<string, string>();

        await WriteStateAsync();
        return Result<Guid>.Success(State.Id);
    }

    public async Task<Result> UpdateAsync(UpdateProductCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Product not found");

        if (!string.IsNullOrWhiteSpace(command.Name))
            State.Name = command.Name;

        if (command.Description != null)
            State.Description = command.Description;

        if (command.StandardCost.HasValue)
            State.StandardCost = command.StandardCost.Value;

        if (command.ListPrice.HasValue)
            State.ListPrice = command.ListPrice.Value;

        if (command.MinimumStock.HasValue)
            State.MinimumStock = command.MinimumStock.Value;

        if (command.MaximumStock.HasValue)
            State.MaximumStock = command.MaximumStock.Value;

        if (command.ReorderPoint.HasValue)
            State.ReorderPoint = command.ReorderPoint.Value;

        if (command.ReorderQuantity.HasValue)
            State.ReorderQuantity = command.ReorderQuantity.Value;

        if (command.Attributes != null)
        {
            foreach (var attr in command.Attributes)
            {
                State.Attributes[attr.Key] = attr.Value;
            }
        }

        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> SetActiveStatusAsync(bool isActive)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Product not found");

        State.IsActive = isActive;
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<StockInfo>> GetStockInfoAsync(Guid? warehouseId = null)
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<StockInfo>.Failure("Product not found"));

        if (!State.TrackInventory)
        {
            var noTrackingInfo = new StockInfo
            {
                TotalStock = 0,
                AvailableStock = 0,
                ReservedStock = 0,
                WarehouseDetails = new Dictionary<Guid, WarehouseStock>()
            };
            return Task.FromResult(Result<StockInfo>.Success(noTrackingInfo));
        }

        var totalStock = State.WarehouseStock.Values.Sum();
        var totalReserved = State.ReservedStock.Values.Sum();
        var availableStock = totalStock - totalReserved;

        var warehouseDetails = new Dictionary<Guid, WarehouseStock>();
        foreach (var warehouseStock in State.WarehouseStock)
        {
            if (Guid.TryParse(warehouseStock.Key, out var whId))
            {
                var reserved = State.ReservedStock.GetValueOrDefault(warehouseStock.Key, 0);
                warehouseDetails[whId] = new WarehouseStock
                {
                    WarehouseId = whId,
                    WarehouseName = $"Warehouse {whId}",
                    OnHand = warehouseStock.Value,
                    Reserved = reserved,
                    Available = warehouseStock.Value - reserved
                };
            }
        }

        var stockInfo = new StockInfo
        {
            TotalStock = totalStock,
            AvailableStock = availableStock,
            ReservedStock = totalReserved,
            WarehouseDetails = warehouseDetails
        };

        return Task.FromResult(Result<StockInfo>.Success(stockInfo));
    }

    public async Task<Result> AdjustStockAsync(StockAdjustmentCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Product not found");

        if (!State.TrackInventory)
            return Result.Failure("Product does not track inventory");

        var warehouseKey = command.WarehouseId.ToString();
        var currentStock = State.WarehouseStock.GetValueOrDefault(warehouseKey, 0);

        decimal newStock = command.Type switch
        {
            StockAdjustmentType.Increase => currentStock + command.Quantity,
            StockAdjustmentType.Decrease => Math.Max(0, currentStock - command.Quantity),
            StockAdjustmentType.SetQuantity => command.Quantity,
            _ => currentStock
        };

        if (newStock < 0)
            return Result.Failure("Stock cannot be negative");

        State.WarehouseStock[warehouseKey] = newStock;
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> ReserveStockAsync(StockReservationCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Product not found");

        if (!State.TrackInventory)
            return Result.Failure("Product does not track inventory");

        var warehouseKey = command.WarehouseId.ToString();
        var currentStock = State.WarehouseStock.GetValueOrDefault(warehouseKey, 0);
        var currentReserved = State.ReservedStock.GetValueOrDefault(warehouseKey, 0);
        var availableStock = currentStock - currentReserved;

        if (command.Quantity > availableStock)
            return Result.Failure($"Insufficient stock. Available: {availableStock}, Requested: {command.Quantity}");

        State.ReservedStock[warehouseKey] = currentReserved + command.Quantity;
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> ReleaseReservationAsync(Guid reservationId)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Product not found");

        // In a real implementation, we would track individual reservations
        // For this demo, we'll just simulate releasing some stock
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<List<ProductVariant>>> GetVariantsAsync()
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<List<ProductVariant>>.Failure("Product not found"));

        return Task.FromResult(Result<List<ProductVariant>>.Success(State.Variants));
    }

    public async Task<Result> AddVariantAsync(ProductVariant variant)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Product not found");

        if (State.Variants.Any(v => v.SKU == variant.SKU))
            return Result.Failure("Variant with this SKU already exists");

        variant.Id = Guid.NewGuid();
        State.Variants.Add(variant);
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }
}