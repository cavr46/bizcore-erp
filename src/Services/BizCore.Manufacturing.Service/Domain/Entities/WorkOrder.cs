using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Manufacturing.Domain.Entities;

public class WorkOrder : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string WorkOrderNumber { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? CompletionDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal CompletedQuantity { get; private set; }
    public decimal ScrapQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public Guid? BomId { get; private set; }
    public string BomVersion { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public WorkOrderPriority Priority { get; private set; }
    public Guid? AssignedToId { get; private set; }
    public Guid? SupervisorId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public string? Notes { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public decimal ActualCost { get; private set; }
    public decimal EstimatedHours { get; private set; }
    public decimal ActualHours { get; private set; }
    public DateTime? PlannedStartDate { get; private set; }
    public DateTime? PlannedEndDate { get; private set; }
    public bool IsActive { get; private set; }
    
    private readonly List<WorkOrderOperation> _operations = new();
    public IReadOnlyCollection<WorkOrderOperation> Operations => _operations.AsReadOnly();
    
    private readonly List<WorkOrderMaterial> _materials = new();
    public IReadOnlyCollection<WorkOrderMaterial> Materials => _materials.AsReadOnly();
    
    private readonly List<WorkOrderTimeEntry> _timeEntries = new();
    public IReadOnlyCollection<WorkOrderTimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    
    private readonly List<WorkOrderQualityCheck> _qualityChecks = new();
    public IReadOnlyCollection<WorkOrderQualityCheck> QualityChecks => _qualityChecks.AsReadOnly();

    private WorkOrder() { }

    public WorkOrder(
        Guid tenantId,
        string workOrderNumber,
        DateTime orderDate,
        Guid productId,
        string productName,
        decimal orderedQuantity,
        string unitOfMeasure,
        Guid? bomId = null,
        DateTime? dueDate = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        WorkOrderNumber = workOrderNumber;
        OrderDate = orderDate;
        ProductId = productId;
        ProductName = productName;
        OrderedQuantity = orderedQuantity;
        UnitOfMeasure = unitOfMeasure;
        BomId = bomId;
        DueDate = dueDate;
        Status = WorkOrderStatus.Draft;
        Priority = WorkOrderPriority.Medium;
        CompletedQuantity = 0;
        ScrapQuantity = 0;
        ActualCost = 0;
        ActualHours = 0;
        IsActive = true;
        
        AddDomainEvent(new WorkOrderCreatedDomainEvent(Id, TenantId, WorkOrderNumber, ProductId, OrderedQuantity));
    }

    public void SetBillOfMaterials(Guid bomId, string bomVersion)
    {
        BomId = bomId;
        BomVersion = bomVersion;
    }

    public void SetPlannedDates(DateTime plannedStartDate, DateTime plannedEndDate)
    {
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
    }

    public void SetDueDate(DateTime dueDate)
    {
        DueDate = dueDate;
    }

    public void SetPriority(WorkOrderPriority priority)
    {
        Priority = priority;
    }

    public void AssignTo(Guid assignedToId, Guid? supervisorId = null)
    {
        AssignedToId = assignedToId;
        SupervisorId = supervisorId;
    }

    public void SetWarehouse(Guid warehouseId)
    {
        WarehouseId = warehouseId;
    }

    public void AddOperation(
        int operationNumber,
        string operationName,
        Guid workCenterId,
        decimal estimatedHours,
        decimal setupTime,
        bool isCompleted = false)
    {
        var operation = new WorkOrderOperation(
            Id,
            operationNumber,
            operationName,
            workCenterId,
            estimatedHours,
            setupTime,
            isCompleted);
        
        _operations.Add(operation);
        RecalculateEstimates();
    }

    public void UpdateOperation(int operationNumber, decimal actualHours, bool isCompleted)
    {
        var operation = _operations.FirstOrDefault(o => o.OperationNumber == operationNumber);
        if (operation != null)
        {
            operation.UpdateActualHours(actualHours);
            if (isCompleted)
                operation.MarkAsCompleted();
            
            RecalculateActuals();
        }
    }

    public void AddMaterial(
        Guid materialId,
        string materialName,
        decimal requiredQuantity,
        string unitOfMeasure,
        decimal unitCost)
    {
        var material = new WorkOrderMaterial(
            Id,
            materialId,
            materialName,
            requiredQuantity,
            unitOfMeasure,
            unitCost);
        
        _materials.Add(material);
        RecalculateEstimates();
    }

    public void IssueMaterial(Guid materialId, decimal issuedQuantity, DateTime issuedDate)
    {
        var material = _materials.FirstOrDefault(m => m.MaterialId == materialId);
        if (material != null)
        {
            material.IssueQuantity(issuedQuantity, issuedDate);
            RecalculateActuals();
        }
    }

    public void AddTimeEntry(
        Guid employeeId,
        int operationNumber,
        DateTime startTime,
        DateTime endTime,
        decimal hours,
        string? notes = null)
    {
        var timeEntry = new WorkOrderTimeEntry(
            Id,
            employeeId,
            operationNumber,
            startTime,
            endTime,
            hours,
            notes);
        
        _timeEntries.Add(timeEntry);
        RecalculateActuals();
    }

    public void AddQualityCheck(
        int operationNumber,
        string checkType,
        QualityResult result,
        Guid checkedBy,
        DateTime checkDate,
        string? notes = null)
    {
        var qualityCheck = new WorkOrderQualityCheck(
            Id,
            operationNumber,
            checkType,
            result,
            checkedBy,
            checkDate,
            notes);
        
        _qualityChecks.Add(qualityCheck);
    }

    public Result Release()
    {
        if (Status != WorkOrderStatus.Draft)
            return Result.Failure("Only draft work orders can be released");

        if (!_operations.Any())
            return Result.Failure("Work order must have at least one operation");

        Status = WorkOrderStatus.Released;
        AddDomainEvent(new WorkOrderReleasedDomainEvent(Id, TenantId, WorkOrderNumber));
        
        return Result.Success();
    }

    public Result Start()
    {
        if (Status != WorkOrderStatus.Released)
            return Result.Failure("Only released work orders can be started");

        Status = WorkOrderStatus.InProgress;
        StartDate = DateTime.UtcNow;
        AddDomainEvent(new WorkOrderStartedDomainEvent(Id, TenantId, WorkOrderNumber, StartDate.Value));
        
        return Result.Success();
    }

    public Result Complete(decimal completedQuantity, decimal? scrapQuantity = null)
    {
        if (Status != WorkOrderStatus.InProgress)
            return Result.Failure("Only in-progress work orders can be completed");

        CompletedQuantity = completedQuantity;
        ScrapQuantity = scrapQuantity ?? 0;
        Status = WorkOrderStatus.Completed;
        CompletionDate = DateTime.UtcNow;
        
        AddDomainEvent(new WorkOrderCompletedDomainEvent(
            Id, TenantId, WorkOrderNumber, CompletedQuantity, ScrapQuantity, CompletionDate.Value));
        
        return Result.Success();
    }

    public Result Close()
    {
        if (Status != WorkOrderStatus.Completed)
            return Result.Failure("Only completed work orders can be closed");

        Status = WorkOrderStatus.Closed;
        AddDomainEvent(new WorkOrderClosedDomainEvent(Id, TenantId, WorkOrderNumber));
        
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (Status == WorkOrderStatus.Completed || Status == WorkOrderStatus.Closed)
            return Result.Failure("Cannot cancel completed or closed work orders");

        Status = WorkOrderStatus.Cancelled;
        Notes = $"Cancelled: {reason}";
        AddDomainEvent(new WorkOrderCancelledDomainEvent(Id, TenantId, WorkOrderNumber, reason));
        
        return Result.Success();
    }

    public void Hold(string reason)
    {
        Status = WorkOrderStatus.OnHold;
        Notes = $"On Hold: {reason}";
        AddDomainEvent(new WorkOrderOnHoldDomainEvent(Id, TenantId, WorkOrderNumber, reason));
    }

    public void Resume()
    {
        Status = WorkOrderStatus.Released;
        AddDomainEvent(new WorkOrderResumedDomainEvent(Id, TenantId, WorkOrderNumber));
    }

    public decimal GetCompletionPercentage()
    {
        if (OrderedQuantity == 0)
            return 0;

        return (CompletedQuantity / OrderedQuantity) * 100;
    }

    public decimal GetRemainingQuantity()
    {
        return Math.Max(0, OrderedQuantity - CompletedQuantity - ScrapQuantity);
    }

    public decimal GetEfficiencyPercentage()
    {
        if (EstimatedHours == 0)
            return 0;

        return (EstimatedHours / Math.Max(ActualHours, 1)) * 100;
    }

    public bool IsOverdue()
    {
        return DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status != WorkOrderStatus.Completed;
    }

    public TimeSpan? GetLeadTime()
    {
        if (StartDate.HasValue && CompletionDate.HasValue)
            return CompletionDate.Value - StartDate.Value;

        return null;
    }

    private void RecalculateEstimates()
    {
        EstimatedHours = _operations.Sum(o => o.EstimatedHours);
        EstimatedCost = _materials.Sum(m => m.GetTotalCost()) + (EstimatedHours * 25); // $25/hour labor rate
    }

    private void RecalculateActuals()
    {
        ActualHours = _timeEntries.Sum(t => t.Hours);
        ActualCost = _materials.Sum(m => m.GetActualCost()) + (ActualHours * 25); // $25/hour labor rate
    }
}

public class WorkOrderOperation : Entity<Guid>
{
    public Guid WorkOrderId { get; private set; }
    public int OperationNumber { get; private set; }
    public string OperationName { get; private set; }
    public Guid WorkCenterId { get; private set; }
    public decimal EstimatedHours { get; private set; }
    public decimal ActualHours { get; private set; }
    public decimal SetupTime { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public OperationStatus Status { get; private set; }

    private WorkOrderOperation() { }

    public WorkOrderOperation(
        Guid workOrderId,
        int operationNumber,
        string operationName,
        Guid workCenterId,
        decimal estimatedHours,
        decimal setupTime,
        bool isCompleted)
    {
        Id = Guid.NewGuid();
        WorkOrderId = workOrderId;
        OperationNumber = operationNumber;
        OperationName = operationName;
        WorkCenterId = workCenterId;
        EstimatedHours = estimatedHours;
        SetupTime = setupTime;
        IsCompleted = isCompleted;
        Status = OperationStatus.Pending;
    }

    public void UpdateActualHours(decimal actualHours)
    {
        ActualHours = actualHours;
    }

    public void MarkAsCompleted()
    {
        IsCompleted = true;
        CompletedDate = DateTime.UtcNow;
        Status = OperationStatus.Completed;
    }

    public void Start()
    {
        Status = OperationStatus.InProgress;
    }
}

public class WorkOrderMaterial : Entity<Guid>
{
    public Guid WorkOrderId { get; private set; }
    public Guid MaterialId { get; private set; }
    public string MaterialName { get; private set; }
    public decimal RequiredQuantity { get; private set; }
    public decimal IssuedQuantity { get; private set; }
    public decimal ConsumedQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public decimal UnitCost { get; private set; }
    public DateTime? IssuedDate { get; private set; }
    public MaterialStatus Status { get; private set; }

    private WorkOrderMaterial() { }

    public WorkOrderMaterial(
        Guid workOrderId,
        Guid materialId,
        string materialName,
        decimal requiredQuantity,
        string unitOfMeasure,
        decimal unitCost)
    {
        Id = Guid.NewGuid();
        WorkOrderId = workOrderId;
        MaterialId = materialId;
        MaterialName = materialName;
        RequiredQuantity = requiredQuantity;
        UnitOfMeasure = unitOfMeasure;
        UnitCost = unitCost;
        Status = MaterialStatus.NotIssued;
    }

    public void IssueQuantity(decimal quantity, DateTime issuedDate)
    {
        IssuedQuantity += quantity;
        IssuedDate = issuedDate;
        Status = IssuedQuantity >= RequiredQuantity ? MaterialStatus.FullyIssued : MaterialStatus.PartiallyIssued;
    }

    public void ConsumeQuantity(decimal quantity)
    {
        ConsumedQuantity += quantity;
    }

    public decimal GetTotalCost()
    {
        return RequiredQuantity * UnitCost;
    }

    public decimal GetActualCost()
    {
        return IssuedQuantity * UnitCost;
    }

    public decimal GetRemainingQuantity()
    {
        return Math.Max(0, RequiredQuantity - IssuedQuantity);
    }
}

public class WorkOrderTimeEntry : Entity<Guid>
{
    public Guid WorkOrderId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int OperationNumber { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public decimal Hours { get; private set; }
    public string? Notes { get; private set; }

    private WorkOrderTimeEntry() { }

    public WorkOrderTimeEntry(
        Guid workOrderId,
        Guid employeeId,
        int operationNumber,
        DateTime startTime,
        DateTime endTime,
        decimal hours,
        string? notes)
    {
        Id = Guid.NewGuid();
        WorkOrderId = workOrderId;
        EmployeeId = employeeId;
        OperationNumber = operationNumber;
        StartTime = startTime;
        EndTime = endTime;
        Hours = hours;
        Notes = notes;
    }
}

public class WorkOrderQualityCheck : Entity<Guid>
{
    public Guid WorkOrderId { get; private set; }
    public int OperationNumber { get; private set; }
    public string CheckType { get; private set; }
    public QualityResult Result { get; private set; }
    public Guid CheckedBy { get; private set; }
    public DateTime CheckDate { get; private set; }
    public string? Notes { get; private set; }

    private WorkOrderQualityCheck() { }

    public WorkOrderQualityCheck(
        Guid workOrderId,
        int operationNumber,
        string checkType,
        QualityResult result,
        Guid checkedBy,
        DateTime checkDate,
        string? notes)
    {
        Id = Guid.NewGuid();
        WorkOrderId = workOrderId;
        OperationNumber = operationNumber;
        CheckType = checkType;
        Result = result;
        CheckedBy = checkedBy;
        CheckDate = checkDate;
        Notes = notes;
    }
}

// Enums
public class WorkOrderStatus : SmartEnum<WorkOrderStatus>
{
    public static readonly WorkOrderStatus Draft = new(1, nameof(Draft));
    public static readonly WorkOrderStatus Released = new(2, nameof(Released));
    public static readonly WorkOrderStatus InProgress = new(3, nameof(InProgress));
    public static readonly WorkOrderStatus OnHold = new(4, nameof(OnHold));
    public static readonly WorkOrderStatus Completed = new(5, nameof(Completed));
    public static readonly WorkOrderStatus Closed = new(6, nameof(Closed));
    public static readonly WorkOrderStatus Cancelled = new(7, nameof(Cancelled));

    private WorkOrderStatus(int value, string name) : base(name, value) { }
}

public class WorkOrderPriority : SmartEnum<WorkOrderPriority>
{
    public static readonly WorkOrderPriority Low = new(1, nameof(Low));
    public static readonly WorkOrderPriority Medium = new(2, nameof(Medium));
    public static readonly WorkOrderPriority High = new(3, nameof(High));
    public static readonly WorkOrderPriority Critical = new(4, nameof(Critical));

    private WorkOrderPriority(int value, string name) : base(name, value) { }
}

public class OperationStatus : SmartEnum<OperationStatus>
{
    public static readonly OperationStatus Pending = new(1, nameof(Pending));
    public static readonly OperationStatus InProgress = new(2, nameof(InProgress));
    public static readonly OperationStatus Completed = new(3, nameof(Completed));
    public static readonly OperationStatus Cancelled = new(4, nameof(Cancelled));

    private OperationStatus(int value, string name) : base(name, value) { }
}

public class MaterialStatus : SmartEnum<MaterialStatus>
{
    public static readonly MaterialStatus NotIssued = new(1, nameof(NotIssued));
    public static readonly MaterialStatus PartiallyIssued = new(2, nameof(PartiallyIssued));
    public static readonly MaterialStatus FullyIssued = new(3, nameof(FullyIssued));

    private MaterialStatus(int value, string name) : base(name, value) { }
}

public class QualityResult : SmartEnum<QualityResult>
{
    public static readonly QualityResult Pass = new(1, nameof(Pass));
    public static readonly QualityResult Fail = new(2, nameof(Fail));
    public static readonly QualityResult Rework = new(3, nameof(Rework));

    private QualityResult(int value, string name) : base(name, value) { }
}

// Domain Events
public record WorkOrderCreatedDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    Guid ProductId,
    decimal OrderedQuantity) : INotification;

public record WorkOrderReleasedDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber) : INotification;

public record WorkOrderStartedDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    DateTime StartDate) : INotification;

public record WorkOrderCompletedDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    decimal CompletedQuantity,
    decimal ScrapQuantity,
    DateTime CompletionDate) : INotification;

public record WorkOrderClosedDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber) : INotification;

public record WorkOrderCancelledDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    string Reason) : INotification;

public record WorkOrderOnHoldDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber,
    string Reason) : INotification;

public record WorkOrderResumedDomainEvent(
    Guid WorkOrderId,
    Guid TenantId,
    string WorkOrderNumber) : INotification;