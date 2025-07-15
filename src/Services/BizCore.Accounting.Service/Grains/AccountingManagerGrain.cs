using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace BizCore.Accounting.Grains;

public interface IAccountingManagerGrain : IGrainWithGuidKey
{
    Task<string> GenerateAccountCodeAsync(string accountTypeCode);
    Task<string> GenerateJournalEntryNumberAsync();
    Task ProcessPostedEntryAsync(JournalEntryPostedEvent entryEvent);
    Task<AccountingSummary> GetSummaryAsync();
    Task<bool> CanPostToAccountAsync(Guid accountId);
    Task<List<Guid>> GetAccountsByTypeAsync(string accountType);
}

public class AccountingManagerGrain : TenantGrainBase<AccountingManagerState>, 
    IAccountingManagerGrain, IStreamSubscriptionObserver
{
    private IAsyncStream<JournalEntryPostedEvent>? _stream;

    public AccountingManagerGrain([PersistentState("accountingManager", "Default")] IPersistentState<AccountingManagerState> state)
        : base(state)
    {
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        var streamProvider = this.GetStreamProvider("Default");
        _stream = streamProvider.GetStream<JournalEntryPostedEvent>(
            StreamId.Create("journal-entries", TenantId.ToString()));
        
        await _stream.SubscribeAsync(this);
    }

    public async Task<string> GenerateAccountCodeAsync(string accountTypeCode)
    {
        var nextNumber = _state.State.GetNextAccountNumber(accountTypeCode);
        _state.State.IncrementAccountNumber(accountTypeCode);
        await SaveStateAsync();
        
        return $"{accountTypeCode}{nextNumber:D4}";
    }

    public async Task<string> GenerateJournalEntryNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var nextNumber = _state.State.GetNextJournalEntryNumber(year);
        _state.State.IncrementJournalEntryNumber(year);
        await SaveStateAsync();
        
        return $"JE{year}{nextNumber:D6}";
    }

    public async Task ProcessPostedEntryAsync(JournalEntryPostedEvent entryEvent)
    {
        // Update account balances
        foreach (var line in entryEvent.Lines)
        {
            var accountGrain = GrainFactory.GetGrain<IAccountGrain>(
                $"{TenantId}_{line.AccountId}");
            
            await accountGrain.UpdateBalanceAsync(
                entryEvent.Date.Year,
                entryEvent.Date.Month,
                line.DebitAmount,
                line.CreditAmount);
        }

        // Update manager statistics
        _state.State.TotalPostedEntries++;
        _state.State.LastProcessedEntry = entryEvent.EntryId;
        _state.State.LastProcessedAt = DateTime.UtcNow;

        await SaveStateAsync();
    }

    public async Task<AccountingSummary> GetSummaryAsync()
    {
        return new AccountingSummary(
            _state.State.TotalPostedEntries,
            _state.State.LastProcessedAt,
            _state.State.GetTotalAccounts());
    }

    public async Task<bool> CanPostToAccountAsync(Guid accountId)
    {
        var accountGrain = GrainFactory.GetGrain<IAccountGrain>($"{TenantId}_{accountId}");
        var account = await accountGrain.GetAccountAsync();
        
        return account?.IsActive == true && account.AllowManualEntry;
    }

    public async Task<List<Guid>> GetAccountsByTypeAsync(string accountType)
    {
        return _state.State.GetAccountsByType(accountType);
    }

    public async Task OnNextAsync(JournalEntryPostedEvent item, StreamSequenceToken? token = null)
    {
        await ProcessPostedEntryAsync(item);
    }

    public async Task OnCompletedAsync()
    {
        // Stream completed
    }

    public async Task OnErrorAsync(Exception ex)
    {
        // Handle error
    }
}

public class AccountingManagerState
{
    public int TotalPostedEntries { get; set; }
    public Guid? LastProcessedEntry { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public Dictionary<string, int> AccountNumberCounters { get; set; } = new();
    public Dictionary<int, int> JournalEntryNumberCounters { get; set; } = new();
    public Dictionary<string, List<Guid>> AccountsByType { get; set; } = new();

    public int GetNextAccountNumber(string accountTypeCode)
    {
        if (!AccountNumberCounters.TryGetValue(accountTypeCode, out var current))
        {
            current = 0;
        }
        return current + 1;
    }

    public void IncrementAccountNumber(string accountTypeCode)
    {
        if (AccountNumberCounters.TryGetValue(accountTypeCode, out var current))
        {
            AccountNumberCounters[accountTypeCode] = current + 1;
        }
        else
        {
            AccountNumberCounters[accountTypeCode] = 1;
        }
    }

    public int GetNextJournalEntryNumber(int year)
    {
        if (!JournalEntryNumberCounters.TryGetValue(year, out var current))
        {
            current = 0;
        }
        return current + 1;
    }

    public void IncrementJournalEntryNumber(int year)
    {
        if (JournalEntryNumberCounters.TryGetValue(year, out var current))
        {
            JournalEntryNumberCounters[year] = current + 1;
        }
        else
        {
            JournalEntryNumberCounters[year] = 1;
        }
    }

    public void AddAccountToType(string accountType, Guid accountId)
    {
        if (!AccountsByType.TryGetValue(accountType, out var accounts))
        {
            accounts = new List<Guid>();
            AccountsByType[accountType] = accounts;
        }
        accounts.Add(accountId);
    }

    public List<Guid> GetAccountsByType(string accountType)
    {
        return AccountsByType.TryGetValue(accountType, out var accounts) 
            ? accounts 
            : new List<Guid>();
    }

    public int GetTotalAccounts()
    {
        return AccountsByType.Values.Sum(list => list.Count);
    }
}

public record AccountingSummary(
    int TotalPostedEntries,
    DateTime? LastProcessedAt,
    int TotalAccounts);