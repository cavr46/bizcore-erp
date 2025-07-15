using BizCore.Manufacturing.Domain.Entities;
using BizCore.Orleans.Core;
using Microsoft.Extensions.Logging;

namespace BizCore.Manufacturing.Grains;

public interface IWorkOrderGrain : IGrainWithGuidKey
{
    Task<WorkOrder> CreateWorkOrderAsync(CreateWorkOrderRequest request);
    Task<WorkOrder> GetWorkOrderAsync();
    Task<Result> ReleaseWorkOrderAsync();
    Task<Result> StartWorkOrderAsync();
    Task<Result> CompleteWorkOrderAsync(decimal completedQuantity, decimal? scrapQuantity = null);
    Task<Result> CloseWorkOrderAsync();
    Task<Result> CancelWorkOrderAsync(string reason);
    Task HoldWorkOrderAsync(string reason);
    Task ResumeWorkOrderAsync();
    Task AddOperationAsync(AddOperationRequest request);
    Task AddMaterialAsync(AddMaterialRequest request);
    Task IssueMaterialAsync(Guid materialId, decimal quantity);
    Task AddTimeEntryAsync(AddTimeEntryRequest request);
    Task AddQualityCheckAsync(AddQualityCheckRequest request);
    Task<WorkOrderStatistics> GetStatisticsAsync();
}

public class WorkOrderGrain : TenantGrainBase<WorkOrderState>, IWorkOrderGrain
{
    private readonly ILogger<WorkOrderGrain> _logger;

    public WorkOrderGrain(ILogger<WorkOrderGrain> logger)
    {
        _logger = logger;
    }

    public async Task<WorkOrder> CreateWorkOrderAsync(CreateWorkOrderRequest request)
    {
        if (_state.State.WorkOrder != null)
            throw new InvalidOperationException("Work order already exists");

        var workOrder = new WorkOrder(
            GetTenantId(),
            request.WorkOrderNumber,
            request.OrderDate,
            request.ProductId,
            request.ProductName,
            request.OrderedQuantity,
            request.UnitOfMeasure,
            request.BomId,
            request.DueDate);

        if (request.AssignedToId.HasValue)
            workOrder.AssignTo(request.AssignedToId.Value, request.SupervisorId);

        if (request.WarehouseId.HasValue)
            workOrder.SetWarehouse(request.WarehouseId.Value);

        if (request.Priority.HasValue)
            workOrder.SetPriority(request.Priority.Value);

        if (request.PlannedStartDate.HasValue && request.PlannedEndDate.HasValue)
            workOrder.SetPlannedDates(request.PlannedStartDate.Value, request.PlannedEndDate.Value);

        _state.State.WorkOrder = workOrder;
        await SaveStateAsync();

        _logger.LogInformation("Work order {WorkOrderNumber} created for tenant {TenantId}", 
            request.WorkOrderNumber, GetTenantId());

        return workOrder;
    }

    public Task<WorkOrder> GetWorkOrderAsync()
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        return Task.FromResult(_state.State.WorkOrder);
    }

    public async Task<Result> ReleaseWorkOrderAsync()
    {
        if (_state.State.WorkOrder == null)
            return Result.Failure("Work order not found");

        var result = _state.State.WorkOrder.Release();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Work order {WorkOrderNumber} released for tenant {TenantId}", 
                _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> StartWorkOrderAsync()
    {
        if (_state.State.WorkOrder == null)
            return Result.Failure("Work order not found");

        var result = _state.State.WorkOrder.Start();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Work order {WorkOrderNumber} started for tenant {TenantId}", 
                _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> CompleteWorkOrderAsync(decimal completedQuantity, decimal? scrapQuantity = null)
    {
        if (_state.State.WorkOrder == null)
            return Result.Failure("Work order not found");

        var result = _state.State.WorkOrder.Complete(completedQuantity, scrapQuantity);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Work order {WorkOrderNumber} completed for tenant {TenantId}", 
                _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> CloseWorkOrderAsync()
    {
        if (_state.State.WorkOrder == null)
            return Result.Failure("Work order not found");

        var result = _state.State.WorkOrder.Close();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Work order {WorkOrderNumber} closed for tenant {TenantId}", 
                _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
        }

        return result;
    }

    public async Task<Result> CancelWorkOrderAsync(string reason)
    {
        if (_state.State.WorkOrder == null)
            return Result.Failure("Work order not found");

        var result = _state.State.WorkOrder.Cancel(reason);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            _logger.LogInformation("Work order {WorkOrderNumber} cancelled for tenant {TenantId}: {Reason}", 
                _state.State.WorkOrder.WorkOrderNumber, GetTenantId(), reason);
        }

        return result;
    }

    public async Task HoldWorkOrderAsync(string reason)
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.Hold(reason);
        await SaveStateAsync();

        _logger.LogInformation("Work order {WorkOrderNumber} put on hold for tenant {TenantId}: {Reason}", 
            _state.State.WorkOrder.WorkOrderNumber, GetTenantId(), reason);
    }

    public async Task ResumeWorkOrderAsync()
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.Resume();
        await SaveStateAsync();

        _logger.LogInformation("Work order {WorkOrderNumber} resumed for tenant {TenantId}", 
            _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
    }

    public async Task AddOperationAsync(AddOperationRequest request)
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.AddOperation(
            request.OperationNumber,
            request.OperationName,
            request.WorkCenterId,
            request.EstimatedHours,
            request.SetupTime,
            request.IsCompleted);

        await SaveStateAsync();

        _logger.LogInformation("Operation {OperationNumber} added to work order {WorkOrderNumber} for tenant {TenantId}", 
            request.OperationNumber, _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
    }

    public async Task AddMaterialAsync(AddMaterialRequest request)
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.AddMaterial(
            request.MaterialId,
            request.MaterialName,
            request.RequiredQuantity,
            request.UnitOfMeasure,
            request.UnitCost);

        await SaveStateAsync();

        _logger.LogInformation("Material {MaterialName} added to work order {WorkOrderNumber} for tenant {TenantId}", 
            request.MaterialName, _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
    }

    public async Task IssueMaterialAsync(Guid materialId, decimal quantity)
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.IssueMaterial(materialId, quantity, DateTime.UtcNow);
        await SaveStateAsync();

        _logger.LogInformation("Material {MaterialId} issued to work order {WorkOrderNumber} for tenant {TenantId}", 
            materialId, _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
    }

    public async Task AddTimeEntryAsync(AddTimeEntryRequest request)
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.AddTimeEntry(
            request.EmployeeId,
            request.OperationNumber,
            request.StartTime,
            request.EndTime,
            request.Hours,
            request.Notes);

        await SaveStateAsync();

        _logger.LogInformation("Time entry added to work order {WorkOrderNumber} for tenant {TenantId}", 
            _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
    }

    public async Task AddQualityCheckAsync(AddQualityCheckRequest request)
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        _state.State.WorkOrder.AddQualityCheck(
            request.OperationNumber,
            request.CheckType,
            request.Result,
            request.CheckedBy,
            request.CheckDate,
            request.Notes);

        await SaveStateAsync();

        _logger.LogInformation("Quality check added to work order {WorkOrderNumber} for tenant {TenantId}", 
            _state.State.WorkOrder.WorkOrderNumber, GetTenantId());
    }

    public Task<WorkOrderStatistics> GetStatisticsAsync()
    {
        if (_state.State.WorkOrder == null)
            throw new InvalidOperationException("Work order not found");

        var workOrder = _state.State.WorkOrder;
        var statistics = new WorkOrderStatistics
        {
            WorkOrderId = workOrder.Id,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            Status = workOrder.Status,
            CompletionPercentage = workOrder.GetCompletionPercentage(),
            RemainingQuantity = workOrder.GetRemainingQuantity(),
            EfficiencyPercentage = workOrder.GetEfficiencyPercentage(),
            IsOverdue = workOrder.IsOverdue(),
            LeadTime = workOrder.GetLeadTime(),
            EstimatedCost = workOrder.EstimatedCost,
            ActualCost = workOrder.ActualCost,
            EstimatedHours = workOrder.EstimatedHours,
            ActualHours = workOrder.ActualHours,
            OperationsCompleted = workOrder.Operations.Count(o => o.IsCompleted),
            TotalOperations = workOrder.Operations.Count,
            MaterialsIssued = workOrder.Materials.Count(m => m.Status != MaterialStatus.NotIssued),
            TotalMaterials = workOrder.Materials.Count
        };

        return Task.FromResult(statistics);
    }
}

[GenerateSerializer]
public class WorkOrderState
{
    [Id(0)]
    public WorkOrder? WorkOrder { get; set; }
}

// Request DTOs
public record CreateWorkOrderRequest(
    string WorkOrderNumber,
    DateTime OrderDate,
    Guid ProductId,
    string ProductName,
    decimal OrderedQuantity,
    string UnitOfMeasure,
    Guid? BomId = null,
    DateTime? DueDate = null,
    Guid? AssignedToId = null,
    Guid? SupervisorId = null,
    Guid? WarehouseId = null,
    WorkOrderPriority? Priority = null,
    DateTime? PlannedStartDate = null,
    DateTime? PlannedEndDate = null);

public record AddOperationRequest(
    int OperationNumber,
    string OperationName,
    Guid WorkCenterId,
    decimal EstimatedHours,
    decimal SetupTime,
    bool IsCompleted = false);

public record AddMaterialRequest(
    Guid MaterialId,
    string MaterialName,
    decimal RequiredQuantity,
    string UnitOfMeasure,
    decimal UnitCost);

public record AddTimeEntryRequest(
    Guid EmployeeId,
    int OperationNumber,
    DateTime StartTime,
    DateTime EndTime,
    decimal Hours,
    string? Notes = null);

public record AddQualityCheckRequest(
    int OperationNumber,
    string CheckType,
    QualityResult Result,
    Guid CheckedBy,
    DateTime CheckDate,
    string? Notes = null);

public record WorkOrderStatistics(
    Guid WorkOrderId,
    string WorkOrderNumber,
    WorkOrderStatus Status,
    decimal CompletionPercentage,
    decimal RemainingQuantity,
    decimal EfficiencyPercentage,
    bool IsOverdue,
    TimeSpan? LeadTime,
    decimal EstimatedCost,
    decimal ActualCost,
    decimal EstimatedHours,
    decimal ActualHours,
    int OperationsCompleted,
    int TotalOperations,
    int MaterialsIssued,
    int TotalMaterials);