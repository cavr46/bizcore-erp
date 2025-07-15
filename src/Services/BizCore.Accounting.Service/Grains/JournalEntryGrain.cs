using BizCore.Accounting.Domain.Entities;
using BizCore.Orleans.Contracts;
using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace BizCore.Accounting.Grains;

public interface IJournalEntryGrain : IGrainWithStringKey
{
    Task<JournalEntry?> GetEntryAsync();
    Task<JournalEntry> CreateEntryAsync(CreateJournalEntryRequest request);
    Task AddLineAsync(AddJournalLineRequest request);
    Task RemoveLineAsync(int lineNumber);
    Task<Result> ValidateAsync();
    Task<Result> SubmitAsync();
    Task<Result> ApproveAsync(string approvedBy);
    Task<Result> PostAsync(string postedBy);
    Task<Result<JournalEntry>> CreateReversalAsync(CreateReversalRequest request);
    Task AddAttachmentAsync(AddAttachmentRequest request);
}

public class JournalEntryGrain : TenantGrainBase<JournalEntryState>, IJournalEntryGrain
{
    private IAsyncStream<JournalEntryPostedEvent>? _stream;

    public JournalEntryGrain([PersistentState("journalEntry", "Default")] IPersistentState<JournalEntryState> state)
        : base(state)
    {
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        var streamProvider = this.GetStreamProvider("Default");
        _stream = streamProvider.GetStream<JournalEntryPostedEvent>(
            StreamId.Create("journal-entries", TenantId.ToString()));
    }

    public async Task<JournalEntry?> GetEntryAsync()
    {
        return _state.State.Entry;
    }

    public async Task<JournalEntry> CreateEntryAsync(CreateJournalEntryRequest request)
    {
        if (_state.State.Entry != null)
            throw new InvalidOperationException("Journal entry already exists");

        var entry = new JournalEntry(
            request.TenantId,
            request.EntryNumber,
            request.Date,
            request.Description,
            request.Type);

        _state.State.Entry = entry;
        await SaveStateAsync();

        return entry;
    }

    public async Task AddLineAsync(AddJournalLineRequest request)
    {
        if (_state.State.Entry == null)
            throw new InvalidOperationException("Journal entry not found");

        _state.State.Entry.AddLine(
            request.AccountId,
            request.Description,
            request.DebitAmount,
            request.CreditAmount,
            request.CostCenterId,
            request.ProjectId);

        await SaveStateAsync();
    }

    public async Task RemoveLineAsync(int lineNumber)
    {
        if (_state.State.Entry == null)
            throw new InvalidOperationException("Journal entry not found");

        _state.State.Entry.RemoveLine(lineNumber);
        await SaveStateAsync();
    }

    public async Task<Result> ValidateAsync()
    {
        if (_state.State.Entry == null)
            return Result.Failure("Journal entry not found");

        return _state.State.Entry.Validate();
    }

    public async Task<Result> SubmitAsync()
    {
        if (_state.State.Entry == null)
            return Result.Failure("Journal entry not found");

        var result = _state.State.Entry.Submit();
        if (result.IsSuccess)
        {
            await SaveStateAsync();
        }

        return result;
    }

    public async Task<Result> ApproveAsync(string approvedBy)
    {
        if (_state.State.Entry == null)
            return Result.Failure("Journal entry not found");

        var result = _state.State.Entry.Approve(approvedBy);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
        }

        return result;
    }

    public async Task<Result> PostAsync(string postedBy)
    {
        if (_state.State.Entry == null)
            return Result.Failure("Journal entry not found");

        var result = _state.State.Entry.Post(postedBy);
        if (result.IsSuccess)
        {
            await SaveStateAsync();
            await PublishPostedEventAsync(postedBy);
        }

        return result;
    }

    public async Task<Result<JournalEntry>> CreateReversalAsync(CreateReversalRequest request)
    {
        if (_state.State.Entry == null)
            return Result.Failure<JournalEntry>("Journal entry not found");

        var result = _state.State.Entry.CreateReversal(
            request.ReversalNumber,
            request.ReversalDate,
            request.Reason);

        if (result.IsSuccess)
        {
            await SaveStateAsync();
        }

        return result;
    }

    public async Task AddAttachmentAsync(AddAttachmentRequest request)
    {
        if (_state.State.Entry == null)
            throw new InvalidOperationException("Journal entry not found");

        _state.State.Entry.AddAttachment(
            request.FileName,
            request.FileUrl,
            request.UploadedBy);

        await SaveStateAsync();
    }

    private async Task PublishPostedEventAsync(string postedBy)
    {
        if (_stream == null || _state.State.Entry == null)
            return;

        var eventData = new JournalEntryPostedEvent(
            _state.State.Entry.Id,
            TenantId,
            _state.State.Entry.EntryNumber,
            _state.State.Entry.Date,
            postedBy,
            _state.State.Entry.Lines.Select(l => new PostedLineEvent(
                l.AccountId,
                l.DebitAmount?.Amount ?? 0,
                l.CreditAmount?.Amount ?? 0,
                l.CostCenterId,
                l.ProjectId)).ToList());

        await _stream.OnNextAsync(eventData);
    }
}

public class JournalEntryState
{
    public JournalEntry? Entry { get; set; }
}

public record CreateJournalEntryRequest(
    Guid TenantId,
    string EntryNumber,
    DateTime Date,
    string Description,
    EntryType Type);

public record AddJournalLineRequest(
    Guid AccountId,
    string Description,
    Domain.Common.Money? DebitAmount,
    Domain.Common.Money? CreditAmount,
    Guid? CostCenterId,
    Guid? ProjectId);

public record CreateReversalRequest(
    string ReversalNumber,
    DateTime ReversalDate,
    string Reason);

public record AddAttachmentRequest(
    string FileName,
    string FileUrl,
    string UploadedBy);

public record JournalEntryPostedEvent(
    Guid EntryId,
    Guid TenantId,
    string EntryNumber,
    DateTime Date,
    string PostedBy,
    List<PostedLineEvent> Lines);

public record PostedLineEvent(
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    Guid? CostCenterId,
    Guid? ProjectId);