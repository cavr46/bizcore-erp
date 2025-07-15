using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Accounting;
using BizCore.Orleans.Core.Base;

namespace BizCore.Accounting.Service.Grains;

public class JournalEntryGrain : TenantGrainBase<JournalEntryState>, IJournalEntryGrain
{
    public JournalEntryGrain(
        [PersistentState("journalEntry", "AccountingStore")] IPersistentState<JournalEntryState> state)
        : base(state)
    {
    }

    public async Task<Result<Guid>> CreateAsync(CreateJournalEntryCommand command)
    {
        if (State.Id != Guid.Empty)
            return Result<Guid>.Failure("Journal entry already exists");

        if (command.Lines.Count == 0)
            return Result<Guid>.Failure("Journal entry must have at least one line");

        // Validate that debits equal credits
        var totalDebits = command.Lines.Sum(l => l.DebitAmount);
        var totalCredits = command.Lines.Sum(l => l.CreditAmount);

        if (totalDebits != totalCredits)
            return Result<Guid>.Failure("Debits must equal credits");

        // Generate entry number
        var entryNumber = await GenerateEntryNumberAsync();

        State.Id = this.GetPrimaryKey();
        State.TenantId = command.TenantId;
        State.EntryNumber = entryNumber;
        State.EntryDate = command.EntryDate;
        State.Description = command.Description;
        State.Reference = command.Reference;
        State.Status = JournalEntryStatus.Draft;
        State.TotalDebits = totalDebits;
        State.TotalCredits = totalCredits;
        State.CreatedBy = command.CreatedBy;
        State.CreatedAt = DateTime.UtcNow;
        State.Metadata = command.Metadata ?? new Dictionary<string, string>();

        // Create lines
        State.Lines = new List<JournalEntryLine>();
        for (int i = 0; i < command.Lines.Count; i++)
        {
            var cmdLine = command.Lines[i];
            var line = new JournalEntryLine
            {
                LineNumber = i + 1,
                AccountCode = cmdLine.AccountCode,
                Description = cmdLine.Description,
                DebitAmount = cmdLine.DebitAmount,
                CreditAmount = cmdLine.CreditAmount,
                CostCenterId = cmdLine.CostCenterId,
                ProjectId = cmdLine.ProjectId,
                Dimensions = cmdLine.Dimensions ?? new Dictionary<string, string>()
            };
            State.Lines.Add(line);
        }

        await WriteStateAsync();
        return Result<Guid>.Success(State.Id);
    }

    public async Task<Result> PostAsync(string postedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Journal entry not found");

        if (State.Status != JournalEntryStatus.Approved)
            return Result.Failure("Journal entry must be approved before posting");

        if (State.IsReversed)
            return Result.Failure("Cannot post a reversed journal entry");

        // Post to all affected accounts
        var grainFactory = GrainFactory;
        var tasks = new List<Task<Result>>();

        foreach (var line in State.Lines)
        {
            var accountGrain = grainFactory.GetGrain<IAccountGrain>(Guid.NewGuid(), line.AccountCode);
            
            if (line.DebitAmount > 0)
            {
                var debitCommand = new PostTransactionCommand
                {
                    TransactionId = Guid.NewGuid(),
                    Amount = line.DebitAmount,
                    Type = TransactionType.Debit,
                    TransactionDate = State.EntryDate,
                    Description = line.Description,
                    Reference = State.EntryNumber,
                    PostedBy = postedBy
                };
                tasks.Add(accountGrain.PostTransactionAsync(debitCommand));
            }

            if (line.CreditAmount > 0)
            {
                var creditCommand = new PostTransactionCommand
                {
                    TransactionId = Guid.NewGuid(),
                    Amount = line.CreditAmount,
                    Type = TransactionType.Credit,
                    TransactionDate = State.EntryDate,
                    Description = line.Description,
                    Reference = State.EntryNumber,
                    PostedBy = postedBy
                };
                tasks.Add(accountGrain.PostTransactionAsync(creditCommand));
            }
        }

        var results = await Task.WhenAll(tasks);
        var failedResults = results.Where(r => !r.IsSuccess).ToList();

        if (failedResults.Any())
        {
            var errors = string.Join(", ", failedResults.Select(r => r.Error));
            return Result.Failure($"Failed to post to accounts: {errors}");
        }

        State.Status = JournalEntryStatus.Posted;
        State.PostedBy = postedBy;
        State.PostedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> ReverseAsync(string reversedBy, string reason)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Journal entry not found");

        if (State.Status != JournalEntryStatus.Posted)
            return Result.Failure("Only posted entries can be reversed");

        if (State.IsReversed)
            return Result.Failure("Journal entry is already reversed");

        // Create reversal entry
        var reversalEntryId = Guid.NewGuid();
        var reversalGrain = GrainFactory.GetGrain<IJournalEntryGrain>(reversalEntryId);

        var reversalLines = State.Lines.Select(line => new CreateJournalEntryLineCommand
        {
            AccountCode = line.AccountCode,
            Description = $"Reversal of {line.Description}",
            DebitAmount = line.CreditAmount,  // Swap debit and credit
            CreditAmount = line.DebitAmount,
            CostCenterId = line.CostCenterId,
            ProjectId = line.ProjectId,
            Dimensions = line.Dimensions
        }).ToList();

        var reversalCommand = new CreateJournalEntryCommand
        {
            TenantId = State.TenantId,
            EntryDate = DateTime.UtcNow.Date,
            Description = $"Reversal of {State.EntryNumber}: {reason}",
            Reference = $"REV-{State.EntryNumber}",
            Lines = reversalLines,
            CreatedBy = reversedBy
        };

        var createResult = await reversalGrain.CreateAsync(reversalCommand);
        if (!createResult.IsSuccess)
            return Result.Failure($"Failed to create reversal entry: {createResult.Error}");

        // Auto-approve and post the reversal
        var approveResult = await reversalGrain.ApproveAsync(reversedBy);
        if (!approveResult.IsSuccess)
            return Result.Failure($"Failed to approve reversal entry: {approveResult.Error}");

        var postResult = await reversalGrain.PostAsync(reversedBy);
        if (!postResult.IsSuccess)
            return Result.Failure($"Failed to post reversal entry: {postResult.Error}");

        // Mark this entry as reversed
        State.Status = JournalEntryStatus.Reversed;
        State.IsReversed = true;
        State.ReversalEntryId = reversalEntryId;
        State.ReversalReason = reason;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> ApproveAsync(string approvedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Journal entry not found");

        if (State.Status != JournalEntryStatus.Draft && State.Status != JournalEntryStatus.PendingApproval)
            return Result.Failure("Journal entry is not in a state that can be approved");

        if (State.IsReversed)
            return Result.Failure("Cannot approve a reversed journal entry");

        State.Status = JournalEntryStatus.Approved;
        State.ApprovedBy = approvedBy;
        State.ApprovedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> RejectAsync(string rejectedBy, string reason)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Journal entry not found");

        if (State.Status != JournalEntryStatus.PendingApproval)
            return Result.Failure("Journal entry is not pending approval");

        State.Status = JournalEntryStatus.Rejected;
        State.Metadata["RejectedBy"] = rejectedBy;
        State.Metadata["RejectionReason"] = reason;
        State.Metadata["RejectedAt"] = DateTime.UtcNow.ToString("O");

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<JournalEntryState>> GetDetailsAsync()
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<JournalEntryState>.Failure("Journal entry not found"));

        return Task.FromResult(Result<JournalEntryState>.Success(State));
    }

    private async Task<string> GenerateEntryNumberAsync()
    {
        // Simple numbering scheme - in production, this would use a sequence generator
        var date = DateTime.UtcNow;
        var datePrefix = date.ToString("yyyyMM");
        var random = new Random();
        var sequence = random.Next(1000, 9999);
        return $"JE-{datePrefix}-{sequence}";
    }
}