using Orleans;
using BizCore.Orleans.Contracts.Accounting;

namespace BizCore.ApiGateway.Services;

public interface IAccountingService
{
    Task<AccountState?> GetAccountAsync(Guid accountId);
    Task<Result<Guid>> CreateAccountAsync(AccountInitCommand command);
    Task<Result<JournalEntryState>> CreateJournalEntryAsync(CreateJournalEntryCommand command);
    Task<Result<AccountMovementsState>> GetAccountMovementsAsync(Guid accountId, DateTime startDate, DateTime endDate);
}

public class AccountingService : IAccountingService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AccountingService> _logger;

    public AccountingService(IClusterClient clusterClient, ILogger<AccountingService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<AccountState?> GetAccountAsync(Guid accountId)
    {
        try
        {
            var accountGrain = _clusterClient.GetGrain<IAccountGrain>(accountId);
            return await accountGrain.GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account {AccountId}", accountId);
            return null;
        }
    }

    public async Task<Result<Guid>> CreateAccountAsync(AccountInitCommand command)
    {
        try
        {
            var accountGrain = _clusterClient.GetGrain<IAccountGrain>(Guid.NewGuid());
            return await accountGrain.InitializeAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account {AccountCode}", command.AccountCode);
            return Result<Guid>.Failure($"Failed to create account: {ex.Message}");
        }
    }

    public async Task<Result<JournalEntryState>> CreateJournalEntryAsync(CreateJournalEntryCommand command)
    {
        try
        {
            var journalGrain = _clusterClient.GetGrain<IJournalEntryGrain>(Guid.NewGuid());
            var result = await journalGrain.CreateAsync(command);
            
            if (result.IsSuccess)
            {
                var details = await journalGrain.GetDetailsAsync();
                return details;
            }
            
            return Result<JournalEntryState>.Failure(result.Error ?? "Failed to create journal entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating journal entry");
            return Result<JournalEntryState>.Failure($"Failed to create journal entry: {ex.Message}");
        }
    }

    public async Task<Result<AccountMovementsState>> GetAccountMovementsAsync(Guid accountId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var accountGrain = _clusterClient.GetGrain<IAccountGrain>(accountId);
            return await accountGrain.GetMovementsAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account movements for {AccountId}", accountId);
            return Result<AccountMovementsState>.Failure($"Failed to get account movements: {ex.Message}");
        }
    }
}