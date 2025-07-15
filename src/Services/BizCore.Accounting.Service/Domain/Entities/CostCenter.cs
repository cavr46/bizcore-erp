using BizCore.Domain.Common;

namespace BizCore.Accounting.Domain.Entities;

public class CostCenter : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid? ParentCostCenterId { get; private set; }
    public CostCenter? ParentCostCenter { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? ManagerId { get; private set; }
    public Money? BudgetAmount { get; private set; }
    public DateTime? BudgetStartDate { get; private set; }
    public DateTime? BudgetEndDate { get; private set; }

    private readonly List<CostCenter> _childCostCenters = new();
    public IReadOnlyCollection<CostCenter> ChildCostCenters => _childCostCenters.AsReadOnly();

    private CostCenter() { }

    public CostCenter(
        Guid tenantId,
        string code,
        string name,
        Guid? parentCostCenterId = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Code = code;
        Name = name;
        ParentCostCenterId = parentCostCenterId;
        IsActive = true;
    }

    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetManager(Guid managerId)
    {
        ManagerId = managerId;
    }

    public void SetBudget(Money amount, DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new BusinessRuleValidationException("Budget end date must be after start date");

        BudgetAmount = amount;
        BudgetStartDate = startDate;
        BudgetEndDate = endDate;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

public class Project : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? ProjectManagerId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Money? BudgetAmount { get; private set; }
    public Money? ActualAmount { get; private set; }
    public decimal CompletionPercentage { get; private set; }

    private Project() { }

    public Project(
        Guid tenantId,
        string code,
        string name,
        DateTime startDate)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Code = code;
        Name = name;
        StartDate = startDate;
        Status = ProjectStatus.Active;
        CompletionPercentage = 0;
    }

    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetCustomer(Guid customerId)
    {
        CustomerId = customerId;
    }

    public void SetProjectManager(Guid projectManagerId)
    {
        ProjectManagerId = projectManagerId;
    }

    public void SetBudget(Money amount)
    {
        BudgetAmount = amount;
    }

    public void UpdateActualAmount(Money amount)
    {
        ActualAmount = amount;
    }

    public void UpdateProgress(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new BusinessRuleValidationException("Completion percentage must be between 0 and 100");

        CompletionPercentage = percentage;

        if (percentage == 100 && Status == ProjectStatus.Active)
        {
            Complete();
        }
    }

    public void Complete()
    {
        Status = ProjectStatus.Completed;
        EndDate = DateTime.UtcNow;
        CompletionPercentage = 100;
    }

    public void Cancel()
    {
        Status = ProjectStatus.Cancelled;
        EndDate = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Status == ProjectStatus.Completed)
            throw new BusinessRuleValidationException("Cannot reactivate completed project");

        Status = ProjectStatus.Active;
        EndDate = null;
    }
}

public enum ProjectStatus
{
    Active,
    OnHold,
    Completed,
    Cancelled
}