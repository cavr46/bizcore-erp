using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Inventory.Domain.Entities;

public class Product : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Barcode { get; private set; }
    public ProductType Type { get; private set; }
    public ProductStatus Status { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string BaseUnitOfMeasure { get; private set; }
    public Money? StandardCost { get; private set; }
    public Money? SellingPrice { get; private set; }
    public decimal? Weight { get; private set; }
    public string? WeightUnit { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public string? DimensionUnit { get; private set; }
    public bool IsTrackingSerial { get; private set; }
    public bool IsTrackingLot { get; private set; }
    public bool IsTrackingExpiration { get; private set; }
    public bool IsKitProduct { get; private set; }
    public bool IsActive { get; private set; }
    public int? ReorderPoint { get; private set; }
    public int? MaximumStock { get; private set; }
    public int? SafetyStock { get; private set; }
    public DateTime? LastStockCountDate { get; private set; }

    private readonly List<ProductVariant> _variants = new();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    private readonly List<ProductAttribute> _attributes = new();
    public IReadOnlyCollection<ProductAttribute> Attributes => _attributes.AsReadOnly();

    private readonly List<UnitOfMeasure> _alternativeUnits = new();
    public IReadOnlyCollection<UnitOfMeasure> AlternativeUnits => _alternativeUnits.AsReadOnly();

    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    private readonly List<KitComponent> _kitComponents = new();
    public IReadOnlyCollection<KitComponent> KitComponents => _kitComponents.AsReadOnly();

    private Product() { }

    public Product(
        Guid tenantId,
        string sku,
        string name,
        ProductType type,
        Guid categoryId,
        string baseUnitOfMeasure)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Sku = sku;
        Name = name;
        Type = type;
        CategoryId = categoryId;
        BaseUnitOfMeasure = baseUnitOfMeasure;
        Status = ProductStatus.Active;
        IsActive = true;
        
        AddDomainEvent(new ProductCreatedDomainEvent(Id, TenantId, Sku, Name));
    }

    public void UpdateBasicInfo(string name, string? description, string? barcode)
    {
        Name = name;
        Description = description;
        Barcode = barcode;
    }

    public void UpdatePricing(Money? standardCost, Money? sellingPrice)
    {
        StandardCost = standardCost;
        SellingPrice = sellingPrice;
    }

    public void UpdateDimensions(decimal? weight, string? weightUnit, 
        decimal? length, decimal? width, decimal? height, string? dimensionUnit)
    {
        Weight = weight;
        WeightUnit = weightUnit;
        Length = length;
        Width = width;
        Height = height;
        DimensionUnit = dimensionUnit;
    }

    public void SetTrackingOptions(bool trackSerial, bool trackLot, bool trackExpiration)
    {
        IsTrackingSerial = trackSerial;
        IsTrackingLot = trackLot;
        IsTrackingExpiration = trackExpiration;
    }

    public void SetStockLevels(int? reorderPoint, int? maximumStock, int? safetyStock)
    {
        ReorderPoint = reorderPoint;
        MaximumStock = maximumStock;
        SafetyStock = safetyStock;
    }

    public void AddVariant(string name, string? sku, Dictionary<string, string> attributeValues)
    {
        var variant = new ProductVariant(Id, name, sku, attributeValues);
        _variants.Add(variant);
    }

    public void AddAttribute(string name, string value, ProductAttributeType type)
    {
        var attribute = new ProductAttribute(Id, name, value, type);
        _attributes.Add(attribute);
    }

    public void AddAlternativeUnit(string unitCode, string unitName, decimal conversionFactor)
    {
        if (_alternativeUnits.Any(u => u.UnitCode == unitCode))
            throw new BusinessRuleValidationException($"Unit {unitCode} already exists");

        var unit = new UnitOfMeasure(Id, unitCode, unitName, conversionFactor);
        _alternativeUnits.Add(unit);
    }

    public void AddImage(string imageUrl, string? altText, bool isPrimary)
    {
        if (isPrimary)
        {
            // Remove primary flag from other images
            foreach (var image in _images)
            {
                image.SetPrimary(false);
            }
        }

        var productImage = new ProductImage(Id, imageUrl, altText, isPrimary);
        _images.Add(productImage);
    }

    public void MakeKit(List<KitComponentRequest> components)
    {
        if (components == null || !components.Any())
            throw new BusinessRuleValidationException("Kit must have at least one component");

        IsKitProduct = true;
        _kitComponents.Clear();

        foreach (var component in components)
        {
            var kitComponent = new KitComponent(Id, component.ProductId, component.Quantity, component.UnitOfMeasure);
            _kitComponents.Add(kitComponent);
        }
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateStatus(ProductStatus status)
    {
        Status = status;
        
        if (status == ProductStatus.Discontinued)
        {
            Deactivate();
        }
    }

    public void UpdateLastStockCount()
    {
        LastStockCountDate = DateTime.UtcNow;
    }

    public decimal GetQuantityInBaseUnit(decimal quantity, string fromUnit)
    {
        if (fromUnit == BaseUnitOfMeasure)
            return quantity;

        var unit = _alternativeUnits.FirstOrDefault(u => u.UnitCode == fromUnit);
        if (unit == null)
            throw new BusinessRuleValidationException($"Unit {fromUnit} not found");

        return quantity * unit.ConversionFactor;
    }

    public decimal GetQuantityInUnit(decimal baseQuantity, string toUnit)
    {
        if (toUnit == BaseUnitOfMeasure)
            return baseQuantity;

        var unit = _alternativeUnits.FirstOrDefault(u => u.UnitCode == toUnit);
        if (unit == null)
            throw new BusinessRuleValidationException($"Unit {toUnit} not found");

        return baseQuantity / unit.ConversionFactor;
    }
}

public class ProductVariant : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; }
    public string? Sku { get; private set; }
    public Dictionary<string, string> AttributeValues { get; private set; }
    public bool IsActive { get; private set; }

    private ProductVariant() { }

    public ProductVariant(Guid productId, string name, string? sku, Dictionary<string, string> attributeValues)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        Sku = sku;
        AttributeValues = attributeValues ?? new Dictionary<string, string>();
        IsActive = true;
    }

    public void UpdateName(string name) => Name = name;
    public void UpdateSku(string sku) => Sku = sku;
    public void UpdateAttributeValues(Dictionary<string, string> attributeValues) => AttributeValues = attributeValues;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class ProductAttribute : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; }
    public string Value { get; private set; }
    public ProductAttributeType Type { get; private set; }

    private ProductAttribute() { }

    public ProductAttribute(Guid productId, string name, string value, ProductAttributeType type)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        Value = value;
        Type = type;
    }

    public void UpdateValue(string value) => Value = value;
}

public class UnitOfMeasure : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string UnitCode { get; private set; }
    public string UnitName { get; private set; }
    public decimal ConversionFactor { get; private set; }

    private UnitOfMeasure() { }

    public UnitOfMeasure(Guid productId, string unitCode, string unitName, decimal conversionFactor)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        UnitCode = unitCode;
        UnitName = unitName;
        ConversionFactor = conversionFactor;
    }

    public void UpdateConversionFactor(decimal conversionFactor) => ConversionFactor = conversionFactor;
}

public class ProductImage : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ImageUrl { get; private set; }
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int Order { get; private set; }

    private ProductImage() { }

    public ProductImage(Guid productId, string imageUrl, string? altText, bool isPrimary)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ImageUrl = imageUrl;
        AltText = altText;
        IsPrimary = isPrimary;
        Order = 0;
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
    public void UpdateOrder(int order) => Order = order;
}

public class KitComponent : Entity<Guid>
{
    public Guid KitProductId { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; }

    private KitComponent() { }

    public KitComponent(Guid kitProductId, Guid componentProductId, decimal quantity, string unitOfMeasure)
    {
        Id = Guid.NewGuid();
        KitProductId = kitProductId;
        ComponentProductId = componentProductId;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
    }

    public void UpdateQuantity(decimal quantity) => Quantity = quantity;
}

public class ProductType : SmartEnum<ProductType>
{
    public static readonly ProductType Simple = new(1, nameof(Simple));
    public static readonly ProductType Kit = new(2, nameof(Kit));
    public static readonly ProductType Service = new(3, nameof(Service));
    public static readonly ProductType Digital = new(4, nameof(Digital));

    private ProductType(int value, string name) : base(name, value) { }
}

public class ProductStatus : SmartEnum<ProductStatus>
{
    public static readonly ProductStatus Active = new(1, nameof(Active));
    public static readonly ProductStatus Inactive = new(2, nameof(Inactive));
    public static readonly ProductStatus Discontinued = new(3, nameof(Discontinued));
    public static readonly ProductStatus Draft = new(4, nameof(Draft));

    private ProductStatus(int value, string name) : base(name, value) { }
}

public class ProductAttributeType : SmartEnum<ProductAttributeType>
{
    public static readonly ProductAttributeType Text = new(1, nameof(Text));
    public static readonly ProductAttributeType Number = new(2, nameof(Number));
    public static readonly ProductAttributeType Boolean = new(3, nameof(Boolean));
    public static readonly ProductAttributeType Date = new(4, nameof(Date));
    public static readonly ProductAttributeType List = new(5, nameof(List));

    private ProductAttributeType(int value, string name) : base(name, value) { }
}

public record KitComponentRequest(Guid ProductId, decimal Quantity, string UnitOfMeasure);

public record ProductCreatedDomainEvent(
    Guid ProductId,
    Guid TenantId,
    string Sku,
    string Name) : INotification;