using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Inventory;

public interface IProductGrain : IEntityGrain<ProductState>, ITenantGrain
{
    Task<Result<Guid>> CreateAsync(CreateProductCommand command);
    Task<Result> UpdateAsync(UpdateProductCommand command);
    Task<Result> SetActiveStatusAsync(bool isActive);
    Task<Result<StockInfo>> GetStockInfoAsync(Guid? warehouseId = null);
    Task<Result> AdjustStockAsync(StockAdjustmentCommand command);
    Task<Result> ReserveStockAsync(StockReservationCommand command);
    Task<Result> ReleaseReservationAsync(Guid reservationId);
    Task<Result<List<ProductVariant>>> GetVariantsAsync();
    Task<Result> AddVariantAsync(ProductVariant variant);
}

[GenerateSerializer]
public class ProductState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string SKU { get; set; } = string.Empty;
    [Id(3)] public string Name { get; set; } = string.Empty;
    [Id(4)] public string? Description { get; set; }
    [Id(5)] public ProductType Type { get; set; }
    [Id(6)] public string CategoryCode { get; set; } = string.Empty;
    [Id(7)] public string UnitOfMeasure { get; set; } = string.Empty;
    [Id(8)] public decimal StandardCost { get; set; }
    [Id(9)] public decimal ListPrice { get; set; }
    [Id(10)] public bool IsActive { get; set; }
    [Id(11)] public bool TrackInventory { get; set; }
    [Id(12)] public bool AllowBackorders { get; set; }
    [Id(13)] public decimal MinimumStock { get; set; }
    [Id(14)] public decimal MaximumStock { get; set; }
    [Id(15)] public decimal ReorderPoint { get; set; }
    [Id(16)] public decimal ReorderQuantity { get; set; }
    [Id(17)] public int LeadTimeDays { get; set; }
    [Id(18)] public List<ProductVariant> Variants { get; set; } = new();
    [Id(19)] public Dictionary<string, decimal> WarehouseStock { get; set; } = new();
    [Id(20)] public Dictionary<string, decimal> ReservedStock { get; set; } = new();
    [Id(21)] public DateTime CreatedAt { get; set; }
    [Id(22)] public DateTime? LastModifiedAt { get; set; }
    [Id(23)] public Dictionary<string, string> Attributes { get; set; } = new();
}

[GenerateSerializer]
public class ProductVariant
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string SKU { get; set; } = string.Empty;
    [Id(2)] public string Name { get; set; } = string.Empty;
    [Id(3)] public Dictionary<string, string> Attributes { get; set; } = new();
    [Id(4)] public decimal? Price { get; set; }
    [Id(5)] public decimal? Cost { get; set; }
    [Id(6)] public bool IsActive { get; set; }
}

[GenerateSerializer]
public class CreateProductCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public string SKU { get; set; } = string.Empty;
    [Id(2)] public string Name { get; set; } = string.Empty;
    [Id(3)] public string? Description { get; set; }
    [Id(4)] public ProductType Type { get; set; }
    [Id(5)] public string CategoryCode { get; set; } = string.Empty;
    [Id(6)] public string UnitOfMeasure { get; set; } = string.Empty;
    [Id(7)] public decimal StandardCost { get; set; }
    [Id(8)] public decimal ListPrice { get; set; }
    [Id(9)] public bool TrackInventory { get; set; } = true;
    [Id(10)] public decimal MinimumStock { get; set; }
    [Id(11)] public decimal ReorderPoint { get; set; }
    [Id(12)] public string CreatedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UpdateProductCommand
{
    [Id(0)] public string? Name { get; set; }
    [Id(1)] public string? Description { get; set; }
    [Id(2)] public decimal? StandardCost { get; set; }
    [Id(3)] public decimal? ListPrice { get; set; }
    [Id(4)] public decimal? MinimumStock { get; set; }
    [Id(5)] public decimal? MaximumStock { get; set; }
    [Id(6)] public decimal? ReorderPoint { get; set; }
    [Id(7)] public decimal? ReorderQuantity { get; set; }
    [Id(8)] public Dictionary<string, string>? Attributes { get; set; }
    [Id(9)] public string ModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class StockAdjustmentCommand
{
    [Id(0)] public Guid WarehouseId { get; set; }
    [Id(1)] public decimal Quantity { get; set; }
    [Id(2)] public StockAdjustmentType Type { get; set; }
    [Id(3)] public string Reason { get; set; } = string.Empty;
    [Id(4)] public string Reference { get; set; } = string.Empty;
    [Id(5)] public decimal? Cost { get; set; }
    [Id(6)] public string AdjustedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class StockReservationCommand
{
    [Id(0)] public Guid ReservationId { get; set; }
    [Id(1)] public Guid WarehouseId { get; set; }
    [Id(2)] public decimal Quantity { get; set; }
    [Id(3)] public string Purpose { get; set; } = string.Empty;
    [Id(4)] public DateTime ExpiresAt { get; set; }
    [Id(5)] public string ReservedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class StockInfo
{
    [Id(0)] public decimal TotalStock { get; set; }
    [Id(1)] public decimal AvailableStock { get; set; }
    [Id(2)] public decimal ReservedStock { get; set; }
    [Id(3)] public Dictionary<Guid, WarehouseStock> WarehouseDetails { get; set; } = new();
}

[GenerateSerializer]
public class WarehouseStock
{
    [Id(0)] public Guid WarehouseId { get; set; }
    [Id(1)] public string WarehouseName { get; set; } = string.Empty;
    [Id(2)] public decimal OnHand { get; set; }
    [Id(3)] public decimal Reserved { get; set; }
    [Id(4)] public decimal Available { get; set; }
}

public enum ProductType
{
    Physical = 1,
    Service = 2,
    Digital = 3,
    Bundle = 4,
    Kit = 5
}

public enum StockAdjustmentType
{
    Increase = 1,
    Decrease = 2,
    SetQuantity = 3,
    Transfer = 4
}