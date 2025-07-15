using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Accounting;

public interface IJournalEntryGrain : IEntityGrain<JournalEntryState>, ITenantGrain
{
    Task<Result<Guid>> CreateAsync(CreateJournalEntryCommand command);
    Task<Result> PostAsync(string postedBy);
    Task<Result> ReverseAsync(string reversedBy, string reason);
    Task<Result> ApproveAsync(string approvedBy);
    Task<Result> RejectAsync(string rejectedBy, string reason);
    Task<Result<JournalEntryState>> GetDetailsAsync();
}

[GenerateSerializer]
public class JournalEntryState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string EntryNumber { get; set; } = string.Empty;
    [Id(3)] public DateTime EntryDate { get; set; }
    [Id(4)] public string Description { get; set; } = string.Empty;
    [Id(5)] public string Reference { get; set; } = string.Empty;
    [Id(6)] public JournalEntryStatus Status { get; set; }
    [Id(7)] public List<JournalEntryLine> Lines { get; set; } = new();
    [Id(8)] public decimal TotalDebits { get; set; }
    [Id(9)] public decimal TotalCredits { get; set; }
    [Id(10)] public string CreatedBy { get; set; } = string.Empty;
    [Id(11)] public DateTime CreatedAt { get; set; }
    [Id(12)] public string? PostedBy { get; set; }
    [Id(13)] public DateTime? PostedAt { get; set; }
    [Id(14)] public string? ApprovedBy { get; set; }
    [Id(15)] public DateTime? ApprovedAt { get; set; }
    [Id(16)] public bool IsReversed { get; set; }
    [Id(17)] public Guid? ReversalEntryId { get; set; }
    [Id(18)] public string? ReversalReason { get; set; }
    [Id(19)] public Guid? FiscalPeriodId { get; set; }
    [Id(20)] public Dictionary<string, string> Metadata { get; set; } = new();
}

[GenerateSerializer]
public class JournalEntryLine
{
    [Id(0)] public int LineNumber { get; set; }
    [Id(1)] public string AccountCode { get; set; } = string.Empty;
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public decimal DebitAmount { get; set; }
    [Id(4)] public decimal CreditAmount { get; set; }
    [Id(5)] public Guid? CostCenterId { get; set; }
    [Id(6)] public Guid? ProjectId { get; set; }
    [Id(7)] public Dictionary<string, string> Dimensions { get; set; } = new();
}

[GenerateSerializer]
public class CreateJournalEntryCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public DateTime EntryDate { get; set; }
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public string Reference { get; set; } = string.Empty;
    [Id(4)] public List<CreateJournalEntryLineCommand> Lines { get; set; } = new();
    [Id(5)] public string CreatedBy { get; set; } = string.Empty;
    [Id(6)] public Dictionary<string, string>? Metadata { get; set; }
}

[GenerateSerializer]
public class CreateJournalEntryLineCommand
{
    [Id(0)] public string AccountCode { get; set; } = string.Empty;
    [Id(1)] public string Description { get; set; } = string.Empty;
    [Id(2)] public decimal DebitAmount { get; set; }
    [Id(3)] public decimal CreditAmount { get; set; }
    [Id(4)] public Guid? CostCenterId { get; set; }
    [Id(5)] public Guid? ProjectId { get; set; }
    [Id(6)] public Dictionary<string, string>? Dimensions { get; set; }
}

public enum JournalEntryStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Posted = 3,
    Rejected = 4,
    Reversed = 5
}