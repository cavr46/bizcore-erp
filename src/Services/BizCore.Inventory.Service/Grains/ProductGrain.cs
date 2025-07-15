using BizCore.Inventory.Domain.Entities;
using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;

namespace BizCore.Inventory.Grains;

public interface IProductGrain : IGrainWithStringKey
{
    Task<Product?> GetProductAsync();
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<Product> UpdateProductAsync(UpdateProductRequest request);
    Task UpdatePricingAsync(UpdatePricingRequest request);
    Task UpdateStockLevelsAsync(UpdateStockLevelsRequest request);
    Task AddVariantAsync(AddVariantRequest request);
    Task AddAttributeAsync(AddAttributeRequest request);
    Task AddAlternativeUnitAsync(AddUnitRequest request);
    Task AddImageAsync(AddImageRequest request);
    Task MakeKitAsync(MakeKitRequest request);
    Task ActivateAsync();
    Task DeactivateAsync();
    Task UpdateStatusAsync(ProductStatus status);
    Task<decimal> GetQuantityInBaseUnitAsync(decimal quantity, string fromUnit);
    Task<decimal> GetQuantityInUnitAsync(decimal baseQuantity, string toUnit);
}

public class ProductGrain : TenantGrainBase<ProductState>, IProductGrain
{
    public ProductGrain([PersistentState("product", "Default")] IPersistentState<ProductState> state)
        : base(state)
    {
    }

    public async Task<Product?> GetProductAsync()
    {
        return _state.State.Product;
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        if (_state.State.Product != null)
            throw new InvalidOperationException("Product already exists");

        var product = new Product(
            request.TenantId,
            request.Sku,
            request.Name,
            request.Type,
            request.CategoryId,
            request.BaseUnitOfMeasure);

        if (!string.IsNullOrEmpty(request.Description))
        {
            product.UpdateBasicInfo(request.Name, request.Description, request.Barcode);
        }

        _state.State.Product = product;
        await SaveStateAsync();

        return product;
    }

    public async Task<Product> UpdateProductAsync(UpdateProductRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.UpdateBasicInfo(request.Name, request.Description, request.Barcode);
        await SaveStateAsync();

        return _state.State.Product;
    }

    public async Task UpdatePricingAsync(UpdatePricingRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.UpdatePricing(request.StandardCost, request.SellingPrice);
        await SaveStateAsync();
    }

    public async Task UpdateStockLevelsAsync(UpdateStockLevelsRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.SetStockLevels(
            request.ReorderPoint,
            request.MaximumStock,
            request.SafetyStock);

        await SaveStateAsync();
    }

    public async Task AddVariantAsync(AddVariantRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.AddVariant(request.Name, request.Sku, request.AttributeValues);
        await SaveStateAsync();
    }

    public async Task AddAttributeAsync(AddAttributeRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.AddAttribute(request.Name, request.Value, request.Type);
        await SaveStateAsync();
    }

    public async Task AddAlternativeUnitAsync(AddUnitRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.AddAlternativeUnit(
            request.UnitCode,
            request.UnitName,
            request.ConversionFactor);

        await SaveStateAsync();
    }

    public async Task AddImageAsync(AddImageRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.AddImage(request.ImageUrl, request.AltText, request.IsPrimary);
        await SaveStateAsync();
    }

    public async Task MakeKitAsync(MakeKitRequest request)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.MakeKit(request.Components);
        await SaveStateAsync();
    }

    public async Task ActivateAsync()
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.Activate();
        await SaveStateAsync();
    }

    public async Task DeactivateAsync()
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.Deactivate();
        await SaveStateAsync();
    }

    public async Task UpdateStatusAsync(ProductStatus status)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        _state.State.Product.UpdateStatus(status);
        await SaveStateAsync();
    }

    public async Task<decimal> GetQuantityInBaseUnitAsync(decimal quantity, string fromUnit)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        return _state.State.Product.GetQuantityInBaseUnit(quantity, fromUnit);
    }

    public async Task<decimal> GetQuantityInUnitAsync(decimal baseQuantity, string toUnit)
    {
        if (_state.State.Product == null)
            throw new InvalidOperationException("Product not found");

        return _state.State.Product.GetQuantityInUnit(baseQuantity, toUnit);
    }
}

public class ProductState
{
    public Product? Product { get; set; }
}

public record CreateProductRequest(
    Guid TenantId,
    string Sku,
    string Name,
    string? Description,
    string? Barcode,
    ProductType Type,
    Guid CategoryId,
    string BaseUnitOfMeasure);

public record UpdateProductRequest(
    string Name,
    string? Description,
    string? Barcode);

public record UpdatePricingRequest(
    Domain.Common.Money? StandardCost,
    Domain.Common.Money? SellingPrice);

public record UpdateStockLevelsRequest(
    int? ReorderPoint,
    int? MaximumStock,
    int? SafetyStock);

public record AddVariantRequest(
    string Name,
    string? Sku,
    Dictionary<string, string> AttributeValues);

public record AddAttributeRequest(
    string Name,
    string Value,
    ProductAttributeType Type);

public record AddUnitRequest(
    string UnitCode,
    string UnitName,
    decimal ConversionFactor);

public record AddImageRequest(
    string ImageUrl,
    string? AltText,
    bool IsPrimary);

public record MakeKitRequest(
    List<KitComponentRequest> Components);