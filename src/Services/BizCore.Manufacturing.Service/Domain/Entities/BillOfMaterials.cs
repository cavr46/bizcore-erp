using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Manufacturing.Domain.Entities;

public class BillOfMaterials : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string BomNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal ProductionQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public BomType Type { get; private set; }
    public BomStatus Status { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public int Version { get; private set; }
    public string? Notes { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public decimal EstimatedLabor { get; private set; }
    public decimal EstimatedOverhead { get; private set; }
    public decimal TotalCost { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    
    private readonly List<BomComponent> _components = new();
    public IReadOnlyCollection<BomComponent> Components => _components.AsReadOnly();
    
    private readonly List<BomOperation> _operations = new();
    public IReadOnlyCollection<BomOperation> Operations => _operations.AsReadOnly();

    private BillOfMaterials() { }

    public BillOfMaterials(
        Guid tenantId,
        string bomNumber,
        Guid productId,
        string productName,
        decimal productionQuantity,
        string unitOfMeasure,
        BomType type)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        BomNumber = bomNumber;
        ProductId = productId;
        ProductName = productName;
        ProductionQuantity = productionQuantity;
        UnitOfMeasure = unitOfMeasure;
        Type = type;
        Status = BomStatus.Draft;
        EffectiveDate = DateTime.UtcNow;
        Version = 1;
        IsActive = true;
        
        AddDomainEvent(new BomCreatedDomainEvent(Id, TenantId, BomNumber, ProductId));
    }

    public void AddComponent(
        Guid componentProductId,
        string componentName,
        decimal quantity,
        string unitOfMeasure,
        decimal scrapFactor = 0,
        bool isOptional = false)
    {
        if (Status != BomStatus.Draft)
            throw new BusinessRuleValidationException("Cannot add components to non-draft BOM");

        var sequence = _components.Count + 1;
        var component = new BomComponent(
            Id,
            sequence,
            componentProductId,
            componentName,
            quantity,
            unitOfMeasure,
            scrapFactor,
            isOptional);
        
        _components.Add(component);
        RecalculateCosts();
    }

    public void UpdateComponent(int sequence, decimal quantity, decimal scrapFactor)
    {
        if (Status != BomStatus.Draft)
            throw new BusinessRuleValidationException("Cannot update components in non-draft BOM");

        var component = _components.FirstOrDefault(c => c.Sequence == sequence);
        if (component != null)
        {
            component.UpdateQuantity(quantity, scrapFactor);
            RecalculateCosts();
        }
    }

    public void RemoveComponent(int sequence)
    {
        if (Status != BomStatus.Draft)
            throw new BusinessRuleValidationException("Cannot remove components from non-draft BOM");

        var component = _components.FirstOrDefault(c => c.Sequence == sequence);
        if (component != null)
        {
            _components.Remove(component);
            RenumberComponents();
            RecalculateCosts();
        }
    }

    public void AddOperation(
        int operationNumber,
        string operationName,
        Guid workCenterId,
        decimal setupTime,
        decimal runTime,
        decimal queueTime,
        decimal moveTime,
        bool isOutsourced = false)
    {
        if (Status != BomStatus.Draft)
            throw new BusinessRuleValidationException("Cannot add operations to non-draft BOM");

        var operation = new BomOperation(
            Id,
            operationNumber,
            operationName,
            workCenterId,
            setupTime,
            runTime,
            queueTime,
            moveTime,
            isOutsourced);
        
        _operations.Add(operation);
        RecalculateCosts();
    }

    public void UpdateOperation(
        int operationNumber,
        decimal setupTime,
        decimal runTime,
        decimal queueTime,
        decimal moveTime)
    {
        if (Status != BomStatus.Draft)
            throw new BusinessRuleValidationException("Cannot update operations in non-draft BOM");

        var operation = _operations.FirstOrDefault(o => o.OperationNumber == operationNumber);
        if (operation != null)
        {
            operation.UpdateTimes(setupTime, runTime, queueTime, moveTime);
            RecalculateCosts();
        }
    }

    public void RemoveOperation(int operationNumber)
    {
        if (Status != BomStatus.Draft)
            throw new BusinessRuleValidationException("Cannot remove operations from non-draft BOM");

        var operation = _operations.FirstOrDefault(o => o.OperationNumber == operationNumber);
        if (operation != null)
        {
            _operations.Remove(operation);
            RecalculateCosts();
        }
    }

    public Result Activate()
    {
        if (Status != BomStatus.Draft)
            return Result.Failure("Only draft BOMs can be activated");

        if (!_components.Any())
            return Result.Failure("BOM must have at least one component");

        Status = BomStatus.Active;
        AddDomainEvent(new BomActivatedDomainEvent(Id, TenantId, ProductId));
        
        return Result.Success();
    }

    public void Deactivate()
    {
        Status = BomStatus.Inactive;
        IsActive = false;
        AddDomainEvent(new BomDeactivatedDomainEvent(Id, TenantId, ProductId));
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        AddDomainEvent(new BomSetAsDefaultDomainEvent(Id, TenantId, ProductId));
    }

    public void RemoveDefaultFlag()
    {
        IsDefault = false;
    }

    public BillOfMaterials CreateNewVersion()
    {
        var newBom = new BillOfMaterials(
            TenantId,
            GenerateNewBomNumber(),
            ProductId,
            ProductName,
            ProductionQuantity,
            UnitOfMeasure,
            Type)
        {
            Version = Version + 1,
            EffectiveDate = DateTime.UtcNow,
            Notes = Notes
        };

        // Copy components
        foreach (var component in _components)
        {
            newBom.AddComponent(
                component.ComponentProductId,
                component.ComponentName,
                component.Quantity,
                component.UnitOfMeasure,
                component.ScrapFactor,
                component.IsOptional);
        }

        // Copy operations
        foreach (var operation in _operations)
        {
            newBom.AddOperation(
                operation.OperationNumber,
                operation.OperationName,
                operation.WorkCenterId,
                operation.SetupTime,
                operation.RunTime,
                operation.QueueTime,
                operation.MoveTime,
                operation.IsOutsourced);
        }

        return newBom;
    }

    public MaterialRequirement CalculateMaterialRequirement(decimal requiredQuantity)
    {
        var requirements = new List<ComponentRequirement>();
        
        foreach (var component in _components)
        {
            var totalQuantity = component.GetTotalQuantityRequired(requiredQuantity);
            requirements.Add(new ComponentRequirement(
                component.ComponentProductId,
                component.ComponentName,
                totalQuantity,
                component.UnitOfMeasure,
                component.IsOptional));
        }

        return new MaterialRequirement(ProductId, requiredQuantity, requirements);
    }

    public decimal CalculateLeadTime()
    {
        if (!_operations.Any())
            return 0;

        return _operations.Sum(o => o.SetupTime + o.RunTime + o.QueueTime + o.MoveTime);
    }

    public decimal CalculateProductionCost(decimal quantity)
    {
        var materialCost = EstimatedCost * quantity;
        var laborCost = EstimatedLabor * quantity;
        var overheadCost = EstimatedOverhead * quantity;
        
        return materialCost + laborCost + overheadCost;
    }

    private void RecalculateCosts()
    {
        // This would typically integrate with cost calculation services
        // For now, we'll use placeholder logic
        EstimatedCost = _components.Sum(c => c.GetExtendedCost());
        EstimatedLabor = _operations.Sum(o => o.GetLaborCost());
        EstimatedOverhead = EstimatedLabor * 0.5m; // 50% overhead rate
        TotalCost = EstimatedCost + EstimatedLabor + EstimatedOverhead;
    }

    private void RenumberComponents()
    {
        var orderedComponents = _components.OrderBy(c => c.Sequence).ToList();
        for (int i = 0; i < orderedComponents.Count; i++)
        {
            orderedComponents[i].UpdateSequence(i + 1);
        }
    }

    private string GenerateNewBomNumber()
    {
        return $"{BomNumber}.{Version + 1:D2}";
    }
}

public class BomComponent : Entity<Guid>
{
    public Guid BomId { get; private set; }
    public int Sequence { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public string ComponentName { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public decimal ScrapFactor { get; private set; }
    public bool IsOptional { get; private set; }
    public decimal StandardCost { get; private set; }
    public string? Notes { get; private set; }

    private BomComponent() { }

    public BomComponent(
        Guid bomId,
        int sequence,
        Guid componentProductId,
        string componentName,
        decimal quantity,
        string unitOfMeasure,
        decimal scrapFactor,
        bool isOptional)
    {
        Id = Guid.NewGuid();
        BomId = bomId;
        Sequence = sequence;
        ComponentProductId = componentProductId;
        ComponentName = componentName;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        ScrapFactor = scrapFactor;
        IsOptional = isOptional;
    }

    public void UpdateQuantity(decimal quantity, decimal scrapFactor)
    {
        Quantity = quantity;
        ScrapFactor = scrapFactor;
    }

    public void UpdateSequence(int sequence)
    {
        Sequence = sequence;
    }

    public decimal GetTotalQuantityRequired(decimal productionQuantity)
    {
        var baseQuantity = Quantity * productionQuantity;
        var scrapQuantity = baseQuantity * (ScrapFactor / 100);
        return baseQuantity + scrapQuantity;
    }

    public decimal GetExtendedCost()
    {
        return Quantity * StandardCost;
    }
}

public class BomOperation : Entity<Guid>
{
    public Guid BomId { get; private set; }
    public int OperationNumber { get; private set; }
    public string OperationName { get; private set; }
    public Guid WorkCenterId { get; private set; }
    public decimal SetupTime { get; private set; }
    public decimal RunTime { get; private set; }
    public decimal QueueTime { get; private set; }
    public decimal MoveTime { get; private set; }
    public bool IsOutsourced { get; private set; }
    public decimal StandardRate { get; private set; }
    public string? Notes { get; private set; }

    private BomOperation() { }

    public BomOperation(
        Guid bomId,
        int operationNumber,
        string operationName,
        Guid workCenterId,
        decimal setupTime,
        decimal runTime,
        decimal queueTime,
        decimal moveTime,
        bool isOutsourced)
    {
        Id = Guid.NewGuid();
        BomId = bomId;
        OperationNumber = operationNumber;
        OperationName = operationName;
        WorkCenterId = workCenterId;
        SetupTime = setupTime;
        RunTime = runTime;
        QueueTime = queueTime;
        MoveTime = moveTime;
        IsOutsourced = isOutsourced;
    }

    public void UpdateTimes(decimal setupTime, decimal runTime, decimal queueTime, decimal moveTime)
    {
        SetupTime = setupTime;
        RunTime = runTime;
        QueueTime = queueTime;
        MoveTime = moveTime;
    }

    public decimal GetTotalTime()
    {
        return SetupTime + RunTime + QueueTime + MoveTime;
    }

    public decimal GetLaborCost()
    {
        return (SetupTime + RunTime) * StandardRate;
    }
}

public class BomType : SmartEnum<BomType>
{
    public static readonly BomType Manufacturing = new(1, nameof(Manufacturing));
    public static readonly BomType Engineering = new(2, nameof(Engineering));
    public static readonly BomType Planning = new(3, nameof(Planning));
    public static readonly BomType Costing = new(4, nameof(Costing));

    private BomType(int value, string name) : base(name, value) { }
}

public class BomStatus : SmartEnum<BomStatus>
{
    public static readonly BomStatus Draft = new(1, nameof(Draft));
    public static readonly BomStatus Active = new(2, nameof(Active));
    public static readonly BomStatus Inactive = new(3, nameof(Inactive));
    public static readonly BomStatus Obsolete = new(4, nameof(Obsolete));

    private BomStatus(int value, string name) : base(name, value) { }
}

// Material Requirements Planning DTOs
public class MaterialRequirement
{
    public Guid ProductId { get; set; }
    public decimal RequiredQuantity { get; set; }
    public List<ComponentRequirement> Components { get; set; } = new();

    public MaterialRequirement(Guid productId, decimal requiredQuantity, List<ComponentRequirement> components)
    {
        ProductId = productId;
        RequiredQuantity = requiredQuantity;
        Components = components;
    }
}

public class ComponentRequirement
{
    public Guid ComponentProductId { get; set; }
    public string ComponentName { get; set; }
    public decimal RequiredQuantity { get; set; }
    public string UnitOfMeasure { get; set; }
    public bool IsOptional { get; set; }

    public ComponentRequirement(Guid componentProductId, string componentName, decimal requiredQuantity, string unitOfMeasure, bool isOptional)
    {
        ComponentProductId = componentProductId;
        ComponentName = componentName;
        RequiredQuantity = requiredQuantity;
        UnitOfMeasure = unitOfMeasure;
        IsOptional = isOptional;
    }
}

// Domain Events
public record BomCreatedDomainEvent(
    Guid BomId,
    Guid TenantId,
    string BomNumber,
    Guid ProductId) : INotification;

public record BomActivatedDomainEvent(
    Guid BomId,
    Guid TenantId,
    Guid ProductId) : INotification;

public record BomDeactivatedDomainEvent(
    Guid BomId,
    Guid TenantId,
    Guid ProductId) : INotification;

public record BomSetAsDefaultDomainEvent(
    Guid BomId,
    Guid TenantId,
    Guid ProductId) : INotification;