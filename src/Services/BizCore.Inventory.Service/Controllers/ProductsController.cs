using BizCore.Inventory.Domain.Entities;
using BizCore.Inventory.Grains;
using BizCore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BizCore.Inventory.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUserService _currentUserService;

    public ProductsController(IGrainFactory grainFactory, ICurrentUserService currentUserService)
    {
        _grainFactory = grainFactory;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var managerGrain = _grainFactory.GetGrain<IInventoryManagerGrain>(_currentUserService.TenantId.Value);
        var sku = await managerGrain.GenerateSkuAsync(dto.SkuPrefix ?? "PRD");

        var request = new CreateProductRequest(
            _currentUserService.TenantId.Value,
            sku,
            dto.Name,
            dto.Description,
            dto.Barcode,
            dto.Type,
            dto.CategoryId,
            dto.BaseUnitOfMeasure);

        var productId = Guid.NewGuid();
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        var product = await productGrain.CreateProductAsync(request);

        return Ok(MapToDto(product));
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetProduct(Guid productId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        var product = await productGrain.GetProductAsync();

        if (product == null)
            return NotFound();

        return Ok(MapToDto(product));
    }

    [HttpPut("{productId}")]
    public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new UpdateProductRequest(dto.Name, dto.Description, dto.Barcode);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            var product = await productGrain.UpdateProductAsync(request);
            return Ok(MapToDto(product));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{productId}/pricing")]
    public async Task<IActionResult> UpdatePricing(Guid productId, [FromBody] UpdatePricingDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new UpdatePricingRequest(
            dto.StandardCost != null ? new Domain.Common.Money(dto.StandardCost.Value, dto.Currency) : null,
            dto.SellingPrice != null ? new Domain.Common.Money(dto.SellingPrice.Value, dto.Currency) : null);

        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.UpdatePricingAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{productId}/stock-levels")]
    public async Task<IActionResult> UpdateStockLevels(Guid productId, [FromBody] UpdateStockLevelsDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new UpdateStockLevelsRequest(dto.ReorderPoint, dto.MaximumStock, dto.SafetyStock);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.UpdateStockLevelsAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{productId}/variants")]
    public async Task<IActionResult> AddVariant(Guid productId, [FromBody] AddVariantDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new AddVariantRequest(dto.Name, dto.Sku, dto.AttributeValues);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.AddVariantAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{productId}/attributes")]
    public async Task<IActionResult> AddAttribute(Guid productId, [FromBody] AddAttributeDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new AddAttributeRequest(dto.Name, dto.Value, dto.Type);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.AddAttributeAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{productId}/units")]
    public async Task<IActionResult> AddAlternativeUnit(Guid productId, [FromBody] AddUnitDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new AddUnitRequest(dto.UnitCode, dto.UnitName, dto.ConversionFactor);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.AddAlternativeUnitAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{productId}/images")]
    public async Task<IActionResult> AddImage(Guid productId, [FromBody] AddImageDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new AddImageRequest(dto.ImageUrl, dto.AltText, dto.IsPrimary);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.AddImageAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{productId}/make-kit")]
    public async Task<IActionResult> MakeKit(Guid productId, [FromBody] MakeKitDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var request = new MakeKitRequest(dto.Components);
        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.MakeKitAsync(request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{productId}/activate")]
    public async Task<IActionResult> ActivateProduct(Guid productId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.ActivateAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{productId}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(Guid productId)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.DeactivateAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{productId}/status")]
    public async Task<IActionResult> UpdateStatus(Guid productId, [FromBody] UpdateStatusDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            await productGrain.UpdateStatusAsync(dto.Status);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{productId}/convert-quantity")]
    public async Task<IActionResult> ConvertQuantity(Guid productId, [FromBody] ConvertQuantityDto dto)
    {
        if (!_currentUserService.TenantId.HasValue)
            return BadRequest("Tenant ID is required");

        var productGrain = _grainFactory.GetGrain<IProductGrain>($"{_currentUserService.TenantId}_{productId}");
        
        try
        {
            decimal convertedQuantity;
            
            if (dto.ToBaseUnit)
            {
                convertedQuantity = await productGrain.GetQuantityInBaseUnitAsync(dto.Quantity, dto.FromUnit);
            }
            else
            {
                convertedQuantity = await productGrain.GetQuantityInUnitAsync(dto.Quantity, dto.ToUnit!);
            }
            
            return Ok(new { ConvertedQuantity = convertedQuantity });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Barcode,
            product.Type.Name,
            product.Status.Name,
            product.CategoryId,
            product.BrandId,
            product.BaseUnitOfMeasure,
            product.StandardCost?.Amount,
            product.SellingPrice?.Amount,
            product.StandardCost?.Currency ?? product.SellingPrice?.Currency,
            product.Weight,
            product.WeightUnit,
            product.Length,
            product.Width,
            product.Height,
            product.DimensionUnit,
            product.IsTrackingSerial,
            product.IsTrackingLot,
            product.IsTrackingExpiration,
            product.IsKitProduct,
            product.IsActive,
            product.ReorderPoint,
            product.MaximumStock,
            product.SafetyStock,
            product.LastStockCountDate,
            product.Variants.Select(MapVariantToDto).ToList(),
            product.Attributes.Select(MapAttributeToDto).ToList(),
            product.AlternativeUnits.Select(MapUnitToDto).ToList(),
            product.Images.Select(MapImageToDto).ToList(),
            product.KitComponents.Select(MapKitComponentToDto).ToList());
    }

    private static ProductVariantDto MapVariantToDto(ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.Name,
            variant.Sku,
            variant.AttributeValues,
            variant.IsActive);
    }

    private static ProductAttributeDto MapAttributeToDto(ProductAttribute attribute)
    {
        return new ProductAttributeDto(
            attribute.Id,
            attribute.Name,
            attribute.Value,
            attribute.Type.Name);
    }

    private static UnitOfMeasureDto MapUnitToDto(UnitOfMeasure unit)
    {
        return new UnitOfMeasureDto(
            unit.Id,
            unit.UnitCode,
            unit.UnitName,
            unit.ConversionFactor);
    }

    private static ProductImageDto MapImageToDto(ProductImage image)
    {
        return new ProductImageDto(
            image.Id,
            image.ImageUrl,
            image.AltText,
            image.IsPrimary,
            image.Order);
    }

    private static KitComponentDto MapKitComponentToDto(KitComponent component)
    {
        return new KitComponentDto(
            component.Id,
            component.ComponentProductId,
            component.Quantity,
            component.UnitOfMeasure);
    }
}

// DTOs
public record CreateProductDto(
    string? SkuPrefix,
    string Name,
    string? Description,
    string? Barcode,
    ProductType Type,
    Guid CategoryId,
    string BaseUnitOfMeasure);

public record UpdateProductDto(
    string Name,
    string? Description,
    string? Barcode);

public record UpdatePricingDto(
    decimal? StandardCost,
    decimal? SellingPrice,
    string Currency);

public record UpdateStockLevelsDto(
    int? ReorderPoint,
    int? MaximumStock,
    int? SafetyStock);

public record AddVariantDto(
    string Name,
    string? Sku,
    Dictionary<string, string> AttributeValues);

public record AddAttributeDto(
    string Name,
    string Value,
    ProductAttributeType Type);

public record AddUnitDto(
    string UnitCode,
    string UnitName,
    decimal ConversionFactor);

public record AddImageDto(
    string ImageUrl,
    string? AltText,
    bool IsPrimary);

public record MakeKitDto(
    List<KitComponentRequest> Components);

public record UpdateStatusDto(
    ProductStatus Status);

public record ConvertQuantityDto(
    decimal Quantity,
    string FromUnit,
    string? ToUnit,
    bool ToBaseUnit);

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string? Barcode,
    string Type,
    string Status,
    Guid CategoryId,
    Guid? BrandId,
    string BaseUnitOfMeasure,
    decimal? StandardCost,
    decimal? SellingPrice,
    string? Currency,
    decimal? Weight,
    string? WeightUnit,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? DimensionUnit,
    bool IsTrackingSerial,
    bool IsTrackingLot,
    bool IsTrackingExpiration,
    bool IsKitProduct,
    bool IsActive,
    int? ReorderPoint,
    int? MaximumStock,
    int? SafetyStock,
    DateTime? LastStockCountDate,
    List<ProductVariantDto> Variants,
    List<ProductAttributeDto> Attributes,
    List<UnitOfMeasureDto> AlternativeUnits,
    List<ProductImageDto> Images,
    List<KitComponentDto> KitComponents);

public record ProductVariantDto(
    Guid Id,
    string Name,
    string? Sku,
    Dictionary<string, string> AttributeValues,
    bool IsActive);

public record ProductAttributeDto(
    Guid Id,
    string Name,
    string Value,
    string Type);

public record UnitOfMeasureDto(
    Guid Id,
    string UnitCode,
    string UnitName,
    decimal ConversionFactor);

public record ProductImageDto(
    Guid Id,
    string ImageUrl,
    string? AltText,
    bool IsPrimary,
    int Order);

public record KitComponentDto(
    Guid Id,
    Guid ComponentProductId,
    decimal Quantity,
    string UnitOfMeasure);