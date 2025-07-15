using BizCore.Domain.Common;

namespace BizCore.Accounting.Domain.Entities;

public class FiscalPeriod : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Name { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public PeriodStatus Status { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public string? LockedBy { get; private set; }

    private FiscalPeriod() { }

    public FiscalPeriod(
        Guid tenantId,
        int year,
        int month,
        DateTime startDate,
        DateTime endDate)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Year = year;
        Month = month;
        Name = $"{year}-{month:D2}";
        StartDate = startDate;
        EndDate = endDate;
        Status = PeriodStatus.Open;
    }

    public Result Close(string closedBy)
    {
        if (Status == PeriodStatus.Closed)
            return Result.Failure("Period is already closed");

        if (Status == PeriodStatus.Locked)
            return Result.Failure("Period is locked");

        Status = PeriodStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;

        AddDomainEvent(new FiscalPeriodClosedDomainEvent(Id, TenantId, Year, Month));

        return Result.Success();
    }

    public Result Reopen(string reopenedBy)
    {
        if (Status == PeriodStatus.Locked)
            return Result.Failure("Cannot reopen locked period");

        if (Status == PeriodStatus.Open)
            return Result.Failure("Period is already open");

        Status = PeriodStatus.Open;
        ClosedAt = null;
        ClosedBy = null;

        AddDomainEvent(new FiscalPeriodReopenedDomainEvent(Id, TenantId, Year, Month));

        return Result.Success();
    }

    public Result Lock(string lockedBy)
    {
        if (Status != PeriodStatus.Closed)
            return Result.Failure("Only closed periods can be locked");

        Status = PeriodStatus.Locked;
        LockedAt = DateTime.UtcNow;
        LockedBy = lockedBy;

        return Result.Success();
    }

    public bool CanPostTransactions()
    {
        return Status == PeriodStatus.Open;
    }
}

public enum PeriodStatus
{
    Open,
    Closed,
    Locked
}

public record FiscalPeriodClosedDomainEvent(
    Guid PeriodId,
    Guid TenantId,
    int Year,
    int Month) : INotification;

public record FiscalPeriodReopenedDomainEvent(
    Guid PeriodId,
    Guid TenantId,
    int Year,
    int Month) : INotification;