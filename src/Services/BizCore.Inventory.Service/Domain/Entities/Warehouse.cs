using BizCore.Domain.Common;

namespace BizCore.Inventory.Domain.Entities;

public class Warehouse : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Address Address { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsMainWarehouse { get; private set; }
    public WarehouseType Type { get; private set; }
    public Guid? ManagerId { get; private set; }

    private readonly List<Location> _locations = new();
    public IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();

    private readonly List<WarehouseZone> _zones = new();
    public IReadOnlyCollection<WarehouseZone> Zones => _zones.AsReadOnly();

    private Warehouse() { }

    public Warehouse(
        Guid tenantId,
        string code,
        string name,
        Address address,
        WarehouseType type)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Code = code;
        Name = name;
        Address = address;
        Type = type;
        IsActive = true;
        IsMainWarehouse = false;
        
        AddDomainEvent(new WarehouseCreatedDomainEvent(Id, TenantId, Code, Name));
    }

    public void UpdateBasicInfo(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void UpdateAddress(Address address)
    {
        Address = address;
    }

    public void SetAsMainWarehouse()
    {
        IsMainWarehouse = true;
        AddDomainEvent(new MainWarehouseChangedDomainEvent(Id, TenantId));
    }

    public void RemoveMainWarehouseFlag()
    {
        IsMainWarehouse = false;
    }

    public void SetManager(Guid managerId)
    {
        ManagerId = managerId;
    }

    public void AddZone(string code, string name, string? description)
    {
        if (_zones.Any(z => z.Code == code))
            throw new BusinessRuleValidationException($"Zone with code {code} already exists");

        var zone = new WarehouseZone(Id, code, name, description);
        _zones.Add(zone);
    }

    public void AddLocation(string code, string name, Guid zoneId, LocationType type)
    {
        if (_locations.Any(l => l.Code == code))
            throw new BusinessRuleValidationException($"Location with code {code} already exists");

        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null)
            throw new BusinessRuleValidationException("Zone not found");

        var location = new Location(Id, zoneId, code, name, type);
        _locations.Add(location);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class WarehouseZone : Entity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private WarehouseZone() { }

    public WarehouseZone(Guid warehouseId, string code, string name, string? description)
    {
        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }

    public void UpdateBasicInfo(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class Location : Entity<Guid>
{
    public Guid WarehouseId { get; private set; }
    public Guid ZoneId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public LocationType Type { get; private set; }
    public bool IsActive { get; private set; }
    public int? Row { get; private set; }
    public int? Column { get; private set; }
    public int? Level { get; private set; }
    public decimal? MaxCapacity { get; private set; }
    public decimal? CurrentCapacity { get; private set; }

    private Location() { }

    public Location(Guid warehouseId, Guid zoneId, string code, string name, LocationType type)
    {
        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        ZoneId = zoneId;
        Code = code;
        Name = name;
        Type = type;
        IsActive = true;
        CurrentCapacity = 0;
    }

    public void UpdateBasicInfo(string name, LocationType type)
    {
        Name = name;
        Type = type;
    }

    public void SetCoordinates(int row, int column, int level)
    {
        Row = row;
        Column = column;
        Level = level;
    }

    public void SetCapacity(decimal maxCapacity)
    {
        MaxCapacity = maxCapacity;
    }

    public void UpdateCurrentCapacity(decimal capacity)
    {
        CurrentCapacity = capacity;
    }

    public bool HasAvailableCapacity(decimal requiredCapacity)
    {
        if (!MaxCapacity.HasValue)
            return true;

        return (CurrentCapacity ?? 0) + requiredCapacity <= MaxCapacity.Value;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public string? AdditionalInfo { get; }

    public Address(string street, string city, string state, string postalCode, string country, string? additionalInfo = null)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        AdditionalInfo = additionalInfo;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
        yield return AdditionalInfo;
    }
}

public enum WarehouseType
{
    Distribution,
    Manufacturing,
    Retail,
    ThirdParty,
    Virtual
}

public enum LocationType
{
    Receiving,
    Storage,
    Picking,
    Shipping,
    Quarantine,
    Staging,
    Returns
}

public record WarehouseCreatedDomainEvent(
    Guid WarehouseId,
    Guid TenantId,
    string Code,
    string Name) : INotification;

public record MainWarehouseChangedDomainEvent(
    Guid WarehouseId,
    Guid TenantId) : INotification;