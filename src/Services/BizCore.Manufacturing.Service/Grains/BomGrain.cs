using BizCore.Manufacturing.Domain.Entities;
using BizCore.Orleans.Core;
using Microsoft.Extensions.Logging;

namespace BizCore.Manufacturing.Grains;

public interface IBomGrain : IGrainWithGuidKey
{
    Task<BillOfMaterials> CreateBomAsync(CreateBomRequest request);
    Task<BillOfMaterials> GetBomAsync();
    Task AddComponentAsync(AddComponentRequest request);
    Task UpdateComponentAsync(int sequence, decimal quantity, decimal scrapFactor);
    Task RemoveComponentAsync(int sequence);
    Task AddOperationAsync(AddBomOperationRequest request);
    Task UpdateOperationAsync(int operationNumber, decimal setupTime, decimal runTime, decimal queueTime, decimal moveTime);
    Task RemoveOperationAsync(int operationNumber);
    Task<Result> ActivateAsync();
    Task DeactivateAsync();
    Task SetAsDefaultAsync();
    Task<BillOfMaterials> CreateNewVersionAsync();
    Task<MaterialRequirement> CalculateMaterialRequirementAsync(decimal requiredQuantity);
    Task<decimal> CalculateLeadTimeAsync();
    Task<decimal> CalculateProductionCostAsync(decimal quantity);
}

public class BomGrain : TenantGrainBase<BomState>, IBomGrain
{
    private readonly ILogger<BomGrain> _logger;

    public BomGrain(ILogger<BomGrain> logger)
    {
        _logger = logger;
    }

    public async Task<BillOfMaterials> CreateBomAsync(CreateBomRequest request)
    {
        if (_state.State.BillOfMaterials != null)
            throw new InvalidOperationException("BOM already exists");

        var bom = new BillOfMaterials(
            GetTenantId(),
            request.BomNumber,
            request.ProductId,
            request.ProductName,
            request.ProductionQuantity,
            request.UnitOfMeasure,
            request.Type);

        _state.State.BillOfMaterials = bom;
        await SaveStateAsync();

        _logger.LogInformation("BOM {BomNumber} created for tenant {TenantId}", 
            request.BomNumber, GetTenantId());

        return bom;
    }

    public Task<BillOfMaterials> GetBomAsync()
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        return Task.FromResult(_state.State.BillOfMaterials);
    }

    public async Task AddComponentAsync(AddComponentRequest request)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.AddComponent(
            request.ComponentProductId,
            request.ComponentName,
            request.Quantity,
            request.UnitOfMeasure,
            request.ScrapFactor,
            request.IsOptional);

        await SaveStateAsync();

        _logger.LogInformation("Component {ComponentName} added to BOM {BomNumber} for tenant {TenantId}", 
            request.ComponentName, _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task UpdateComponentAsync(int sequence, decimal quantity, decimal scrapFactor)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.UpdateComponent(sequence, quantity, scrapFactor);
        await SaveStateAsync();

        _logger.LogInformation("Component {Sequence} updated in BOM {BomNumber} for tenant {TenantId}", 
            sequence, _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task RemoveComponentAsync(int sequence)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.RemoveComponent(sequence);
        await SaveStateAsync();

        _logger.LogInformation("Component {Sequence} removed from BOM {BomNumber} for tenant {TenantId}", 
            sequence, _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task AddOperationAsync(AddBomOperationRequest request)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.AddOperation(
            request.OperationNumber,
            request.OperationName,
            request.WorkCenterId,
            request.SetupTime,
            request.RunTime,
            request.QueueTime,
            request.MoveTime,
            request.IsOutsourced);

        await SaveStateAsync();

        _logger.LogInformation("Operation {OperationNumber} added to BOM {BomNumber} for tenant {TenantId}", 
            request.OperationNumber, _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task UpdateOperationAsync(int operationNumber, decimal setupTime, decimal runTime, decimal queueTime, decimal moveTime)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.UpdateOperation(operationNumber, setupTime, runTime, queueTime, moveTime);
        await SaveStateAsync();

        _logger.LogInformation("Operation {OperationNumber} updated in BOM {BomNumber} for tenant {TenantId}", 
            operationNumber, _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task RemoveOperationAsync(int operationNumber)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.RemoveOperation(operationNumber);
        await SaveStateAsync();

        _logger.LogInformation("Operation {OperationNumber} removed from BOM {BomNumber} for tenant {TenantId}", 
            operationNumber, _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task<Result> ActivateAsync()
    {
        if (_state.State.BillOfMaterials == null)
            return Result.Failure("BOM not found");

        var result = _state.State.BillOfMaterials.Activate();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("BOM {BomNumber} activated for tenant {TenantId}", 
                _state.State.BillOfMaterials.BomNumber, GetTenantId());
        }

        return result;
    }

    public async Task DeactivateAsync()
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.Deactivate();
        await SaveStateAsync();

        _logger.LogInformation("BOM {BomNumber} deactivated for tenant {TenantId}", 
            _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task SetAsDefaultAsync()
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        _state.State.BillOfMaterials.SetAsDefault();
        await SaveStateAsync();

        _logger.LogInformation("BOM {BomNumber} set as default for tenant {TenantId}", 
            _state.State.BillOfMaterials.BomNumber, GetTenantId());
    }

    public async Task<BillOfMaterials> CreateNewVersionAsync()
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        var newVersion = _state.State.BillOfMaterials.CreateNewVersion();
        
        // Create a new grain for the new version
        var newBomGrain = GrainFactory.GetGrain<IBomGrain>(newVersion.Id);
        
        // Note: In a real implementation, you would need to handle the new version creation
        // differently, possibly by creating a new grain state
        
        _logger.LogInformation("New BOM version {BomNumber} created from {OriginalBomNumber} for tenant {TenantId}", 
            newVersion.BomNumber, _state.State.BillOfMaterials.BomNumber, GetTenantId());

        return newVersion;
    }

    public Task<MaterialRequirement> CalculateMaterialRequirementAsync(decimal requiredQuantity)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        var requirement = _state.State.BillOfMaterials.CalculateMaterialRequirement(requiredQuantity);
        return Task.FromResult(requirement);
    }

    public Task<decimal> CalculateLeadTimeAsync()
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        var leadTime = _state.State.BillOfMaterials.CalculateLeadTime();
        return Task.FromResult(leadTime);
    }

    public Task<decimal> CalculateProductionCostAsync(decimal quantity)
    {
        if (_state.State.BillOfMaterials == null)
            throw new InvalidOperationException("BOM not found");

        var cost = _state.State.BillOfMaterials.CalculateProductionCost(quantity);
        return Task.FromResult(cost);
    }
}

[GenerateSerializer]
public class BomState
{
    [Id(0)]
    public BillOfMaterials? BillOfMaterials { get; set; }
}

// Request DTOs
public record CreateBomRequest(
    string BomNumber,
    Guid ProductId,
    string ProductName,
    decimal ProductionQuantity,
    string UnitOfMeasure,
    BomType Type);

public record AddComponentRequest(
    Guid ComponentProductId,
    string ComponentName,
    decimal Quantity,
    string UnitOfMeasure,
    decimal ScrapFactor = 0,
    bool IsOptional = false);

public record AddBomOperationRequest(
    int OperationNumber,
    string OperationName,
    Guid WorkCenterId,
    decimal SetupTime,
    decimal RunTime,
    decimal QueueTime,
    decimal MoveTime,
    bool IsOutsourced = false);