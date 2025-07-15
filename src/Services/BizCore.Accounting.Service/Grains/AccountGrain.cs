using BizCore.Accounting.Domain.Entities;
using BizCore.Orleans.Contracts;
using BizCore.Orleans.Core.Base;
using Orleans;
using Orleans.Runtime;

namespace BizCore.Accounting.Grains;

public interface IAccountGrain : IGrainWithStringKey
{
    Task<Account?> GetAccountAsync();
    Task<Account> CreateAccountAsync(CreateAccountRequest request);
    Task<Account> UpdateAccountAsync(UpdateAccountRequest request);
    Task ActivateAccountAsync();
    Task DeactivateAccountAsync();
    Task<AccountBalance> GetBalanceAsync(int year, int month);
    Task UpdateBalanceAsync(int year, int month, decimal debitAmount, decimal creditAmount);
    Task<List<AccountBalance>> GetBalancesAsync(int year);
    Task<decimal> GetCurrentBalanceAsync();
}

public class AccountGrain : TenantGrainBase<AccountState>, IAccountGrain
{
    public AccountGrain([PersistentState("account", "Default")] IPersistentState<AccountState> state)
        : base(state)
    {
    }

    public async Task<Account?> GetAccountAsync()
    {
        return _state.State.Account;
    }

    public async Task<Account> CreateAccountAsync(CreateAccountRequest request)
    {
        if (_state.State.Account != null)
            throw new InvalidOperationException("Account already exists");

        var account = new Account(
            request.TenantId,
            request.Code,
            request.Name,
            request.Type,
            request.Currency,
            request.ParentAccountId);

        _state.State.Account = account;
        await SaveStateAsync();

        return account;
    }

    public async Task<Account> UpdateAccountAsync(UpdateAccountRequest request)
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        _state.State.Account.UpdateDetails(request.Name, request.Description);
        await SaveStateAsync();

        return _state.State.Account;
    }

    public async Task ActivateAccountAsync()
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        _state.State.Account.Activate();
        await SaveStateAsync();
    }

    public async Task DeactivateAccountAsync()
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        _state.State.Account.Deactivate();
        await SaveStateAsync();
    }

    public async Task<AccountBalance> GetBalanceAsync(int year, int month)
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        return _state.State.Account.GetOrCreateBalance(year, month);
    }

    public async Task UpdateBalanceAsync(int year, int month, decimal debitAmount, decimal creditAmount)
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        var balance = _state.State.Account.GetOrCreateBalance(year, month);
        
        if (debitAmount > 0)
            balance.AddDebit(new Domain.Common.Money(debitAmount, _state.State.Account.Currency));
        
        if (creditAmount > 0)
            balance.AddCredit(new Domain.Common.Money(creditAmount, _state.State.Account.Currency));

        await SaveStateAsync();
    }

    public async Task<List<AccountBalance>> GetBalancesAsync(int year)
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        return _state.State.Account.Balances
            .Where(b => b.Year == year)
            .OrderBy(b => b.Month)
            .ToList();
    }

    public async Task<decimal> GetCurrentBalanceAsync()
    {
        if (_state.State.Account == null)
            throw new InvalidOperationException("Account not found");

        var currentDate = DateTime.UtcNow;
        var balance = _state.State.Account.GetOrCreateBalance(currentDate.Year, currentDate.Month);
        
        return balance.ClosingBalance.Amount;
    }
}

public class AccountState
{
    public Account? Account { get; set; }
}

public record CreateAccountRequest(
    Guid TenantId,
    string Code,
    string Name,
    AccountType Type,
    string Currency,
    Guid? ParentAccountId);

public record UpdateAccountRequest(
    string Name,
    string? Description);