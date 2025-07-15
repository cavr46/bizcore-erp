using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Accounting.Domain.Entities;

public class JournalEntry : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string EntryNumber { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public string? Reference { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public EntryType Type { get; private set; }
    public Guid? ReversalOfEntryId { get; private set; }
    public JournalEntry? ReversalOfEntry { get; private set; }
    public Guid? ReversedByEntryId { get; private set; }
    public JournalEntry? ReversedByEntry { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }
    
    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();

    private readonly List<JournalEntryAttachment> _attachments = new();
    public IReadOnlyCollection<JournalEntryAttachment> Attachments => _attachments.AsReadOnly();

    private JournalEntry() { }

    public JournalEntry(
        Guid tenantId,
        string entryNumber,
        DateTime date,
        string description,
        EntryType type)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        EntryNumber = entryNumber;
        Date = date;
        Description = description;
        Type = type;
        Status = JournalEntryStatus.Draft;
        
        AddDomainEvent(new JournalEntryCreatedDomainEvent(Id, TenantId, EntryNumber));
    }

    public void AddLine(
        Guid accountId,
        string description,
        Money? debitAmount,
        Money? creditAmount,
        Guid? costCenterId = null,
        Guid? projectId = null)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new BusinessRuleValidationException("Cannot add lines to non-draft entry");

        if (debitAmount != null && creditAmount != null)
            throw new BusinessRuleValidationException("Line cannot have both debit and credit");

        if (debitAmount == null && creditAmount == null)
            throw new BusinessRuleValidationException("Line must have either debit or credit");

        var lineNumber = _lines.Count + 1;
        var line = new JournalEntryLine(
            Id, 
            lineNumber, 
            accountId, 
            description, 
            debitAmount, 
            creditAmount,
            costCenterId,
            projectId);
        
        _lines.Add(line);
    }

    public void RemoveLine(int lineNumber)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new BusinessRuleValidationException("Cannot remove lines from non-draft entry");

        var line = _lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line != null)
        {
            _lines.Remove(line);
            RenumberLines();
        }
    }

    public Result Validate()
    {
        if (!_lines.Any())
            return Result.Failure("Entry must have at least one line");

        var totalDebits = _lines
            .Where(l => l.DebitAmount != null)
            .Sum(l => l.DebitAmount!.Amount);

        var totalCredits = _lines
            .Where(l => l.CreditAmount != null)
            .Sum(l => l.CreditAmount!.Amount);

        if (totalDebits != totalCredits)
            return Result.Failure($"Entry is not balanced. Debits: {totalDebits}, Credits: {totalCredits}");

        return Result.Success();
    }

    public Result Submit()
    {
        if (Status != JournalEntryStatus.Draft)
            return Result.Failure("Only draft entries can be submitted");

        var validationResult = Validate();
        if (validationResult.IsFailure)
            return validationResult;

        Status = JournalEntryStatus.Submitted;
        
        AddDomainEvent(new JournalEntrySubmittedDomainEvent(Id, TenantId));
        
        return Result.Success();
    }

    public Result Approve(string approvedBy)
    {
        if (Status != JournalEntryStatus.Submitted)
            return Result.Failure("Only submitted entries can be approved");

        Status = JournalEntryStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        
        AddDomainEvent(new JournalEntryApprovedDomainEvent(Id, TenantId, approvedBy));
        
        return Result.Success();
    }

    public Result Post(string postedBy)
    {
        if (Status != JournalEntryStatus.Approved)
            return Result.Failure("Only approved entries can be posted");

        Status = JournalEntryStatus.Posted;
        PostedBy = postedBy;
        PostedAt = DateTime.UtcNow;
        
        AddDomainEvent(new JournalEntryPostedDomainEvent(
            Id, 
            TenantId, 
            postedBy,
            Lines.Select(l => new PostedLineDto(
                l.AccountId,
                l.DebitAmount,
                l.CreditAmount,
                l.CostCenterId,
                l.ProjectId)).ToList()));
        
        return Result.Success();
    }

    public Result<JournalEntry> CreateReversal(string reversalNumber, DateTime reversalDate, string reason)
    {
        if (Status != JournalEntryStatus.Posted)
            return Result.Failure<JournalEntry>("Only posted entries can be reversed");

        if (ReversedByEntryId != null)
            return Result.Failure<JournalEntry>("Entry has already been reversed");

        var reversalEntry = new JournalEntry(
            TenantId,
            reversalNumber,
            reversalDate,
            $"Reversal of {EntryNumber}: {reason}",
            EntryType.Reversal)
        {
            ReversalOfEntryId = Id
        };

        // Create reversal lines (swap debits and credits)
        foreach (var line in Lines)
        {
            reversalEntry.AddLine(
                line.AccountId,
                $"Reversal: {line.Description}",
                line.CreditAmount,  // Credit becomes debit
                line.DebitAmount,   // Debit becomes credit
                line.CostCenterId,
                line.ProjectId);
        }

        ReversedByEntryId = reversalEntry.Id;
        
        AddDomainEvent(new JournalEntryReversedDomainEvent(Id, TenantId, reversalEntry.Id));
        
        return Result.Success(reversalEntry);
    }

    private void RenumberLines()
    {
        var orderedLines = _lines.OrderBy(l => l.LineNumber).ToList();
        for (int i = 0; i < orderedLines.Count; i++)
        {
            orderedLines[i].UpdateLineNumber(i + 1);
        }
    }

    public void AddAttachment(string fileName, string fileUrl, string uploadedBy)
    {
        var attachment = new JournalEntryAttachment(Id, fileName, fileUrl, uploadedBy);
        _attachments.Add(attachment);
    }
}

public class JournalEntryLine : Entity<Guid>
{
    public Guid JournalEntryId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public string Description { get; private set; }
    public Money? DebitAmount { get; private set; }
    public Money? CreditAmount { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? ProjectId { get; private set; }

    private JournalEntryLine() { }

    public JournalEntryLine(
        Guid journalEntryId,
        int lineNumber,
        Guid accountId,
        string description,
        Money? debitAmount,
        Money? creditAmount,
        Guid? costCenterId,
        Guid? projectId)
    {
        Id = Guid.NewGuid();
        JournalEntryId = journalEntryId;
        LineNumber = lineNumber;
        AccountId = accountId;
        Description = description;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        CostCenterId = costCenterId;
        ProjectId = projectId;
    }

    public void UpdateLineNumber(int lineNumber)
    {
        LineNumber = lineNumber;
    }
}

public class JournalEntryAttachment : Entity<Guid>
{
    public Guid JournalEntryId { get; private set; }
    public string FileName { get; private set; }
    public string FileUrl { get; private set; }
    public string UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private JournalEntryAttachment() { }

    public JournalEntryAttachment(
        Guid journalEntryId,
        string fileName,
        string fileUrl,
        string uploadedBy)
    {
        Id = Guid.NewGuid();
        JournalEntryId = journalEntryId;
        FileName = fileName;
        FileUrl = fileUrl;
        UploadedBy = uploadedBy;
        UploadedAt = DateTime.UtcNow;
    }
}

public class JournalEntryStatus : SmartEnum<JournalEntryStatus>
{
    public static readonly JournalEntryStatus Draft = new(1, nameof(Draft));
    public static readonly JournalEntryStatus Submitted = new(2, nameof(Submitted));
    public static readonly JournalEntryStatus Approved = new(3, nameof(Approved));
    public static readonly JournalEntryStatus Posted = new(4, nameof(Posted));
    public static readonly JournalEntryStatus Cancelled = new(5, nameof(Cancelled));

    private JournalEntryStatus(int value, string name) : base(name, value) { }
}

public class EntryType : SmartEnum<EntryType>
{
    public static readonly EntryType Manual = new(1, nameof(Manual));
    public static readonly EntryType System = new(2, nameof(System));
    public static readonly EntryType Adjustment = new(3, nameof(Adjustment));
    public static readonly EntryType Closing = new(4, nameof(Closing));
    public static readonly EntryType Opening = new(5, nameof(Opening));
    public static readonly EntryType Reversal = new(6, nameof(Reversal));

    private EntryType(int value, string name) : base(name, value) { }
}

// Domain Events
public record JournalEntryCreatedDomainEvent(
    Guid EntryId,
    Guid TenantId,
    string EntryNumber) : INotification;

public record JournalEntrySubmittedDomainEvent(
    Guid EntryId,
    Guid TenantId) : INotification;

public record JournalEntryApprovedDomainEvent(
    Guid EntryId,
    Guid TenantId,
    string ApprovedBy) : INotification;

public record JournalEntryPostedDomainEvent(
    Guid EntryId,
    Guid TenantId,
    string PostedBy,
    List<PostedLineDto> Lines) : INotification;

public record PostedLineDto(
    Guid AccountId,
    Money? DebitAmount,
    Money? CreditAmount,
    Guid? CostCenterId,
    Guid? ProjectId);

public record JournalEntryReversedDomainEvent(
    Guid EntryId,
    Guid TenantId,
    Guid ReversalEntryId) : INotification;