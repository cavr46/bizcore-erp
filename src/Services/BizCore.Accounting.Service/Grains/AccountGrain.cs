using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Accounting;
using BizCore.Orleans.Core.Base;

namespace BizCore.Accounting.Service.Grains;

public class AccountGrain : TenantGrainBase<AccountState>, IAccountGrain
{
    private readonly List<AccountMovement> _movements = new();
    
    public AccountGrain(
        [PersistentState("account", "AccountingStore")] IPersistentState<AccountState> state)
        : base(state)
    {
    }

    public async Task<Result<Guid>> InitializeAsync(AccountInitCommand command)
    {
        if (State.Id != Guid.Empty)
            return Result<Guid>.Failure("Account already initialized");

        // Validate account code uniqueness would be done via manager grain
        if (string.IsNullOrWhiteSpace(command.AccountCode))
            return Result<Guid>.Failure("Account code is required");

        if (string.IsNullOrWhiteSpace(command.AccountName))
            return Result<Guid>.Failure("Account name is required");

        // Calculate level based on parent
        var level = 1;
        if (!string.IsNullOrWhiteSpace(command.ParentAccountCode))
        {
            var segments = command.ParentAccountCode.Split('.');
            level = segments.Length + 1;
        }

        State.Id = this.GetPrimaryKey();
        State.TenantId = command.TenantId;
        State.AccountCode = command.AccountCode;
        State.AccountName = command.AccountName;
        State.AccountType = command.AccountType;
        State.ParentAccountCode = command.ParentAccountCode;
        State.Level = level;
        State.IsActive = true;
        State.IsSystemAccount = command.IsSystemAccount;
        State.CurrencyCode = command.CurrencyCode;
        State.CurrentBalance = 0;
        State.CreatedAt = DateTime.UtcNow;
        State.Description = command.Description;
        State.Metadata = new Dictionary<string, string>();

        await WriteStateAsync();
        return Result<Guid>.Success(State.Id);
    }

    public async Task<Result> UpdateAsync(AccountUpdateCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Account not found");

        if (State.IsSystemAccount)
            return Result.Failure("Cannot modify system account");

        State.AccountName = command.AccountName;
        State.Description = command.Description;
        State.LastModifiedAt = DateTime.UtcNow;
        
        if (command.Metadata != null)
        {
            foreach (var kvp in command.Metadata)
            {
                State.Metadata[kvp.Key] = kvp.Value;
            }
        }

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<decimal>> GetBalanceAsync(DateTime? asOfDate = null)
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<decimal>.Failure("Account not found"));

        if (!asOfDate.HasValue)
            return Task.FromResult(Result<decimal>.Success(State.CurrentBalance));

        // Calculate balance as of specific date
        var balance = 0m;
        var relevantMovements = _movements.Where(m => m.Date <= asOfDate.Value).OrderBy(m => m.Date);
        
        foreach (var movement in relevantMovements)
        {
            balance = movement.Balance;
        }

        return Task.FromResult(Result<decimal>.Success(balance));
    }

    public async Task<Result> PostTransactionAsync(PostTransactionCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Account not found");

        if (!State.IsActive)
            return Result.Failure("Account is not active");

        if (command.Amount <= 0)
            return Result.Failure("Transaction amount must be positive");

        // Apply transaction based on account type and transaction type
        var previousBalance = State.CurrentBalance;
        var debitAmount = 0m;
        var creditAmount = 0m;

        if (IsDebitAccount(State.AccountType))
        {
            if (command.Type == TransactionType.Debit)
            {
                State.CurrentBalance += command.Amount;
                debitAmount = command.Amount;
            }
            else
            {
                State.CurrentBalance -= command.Amount;
                creditAmount = command.Amount;
            }
        }
        else
        {
            if (command.Type == TransactionType.Credit)
            {
                State.CurrentBalance += command.Amount;
                creditAmount = command.Amount;
            }
            else
            {
                State.CurrentBalance -= command.Amount;
                debitAmount = command.Amount;
            }
        }

        // Add movement record
        var movement = new AccountMovement
        {
            Id = command.TransactionId,
            Date = command.TransactionDate,
            Description = command.Description,
            Reference = command.Reference,
            Debit = debitAmount,
            Credit = creditAmount,
            Balance = State.CurrentBalance
        };

        _movements.Add(movement);
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> SetActiveStatusAsync(bool isActive)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Account not found");

        if (State.IsSystemAccount && !isActive)
            return Result.Failure("Cannot deactivate system account");

        State.IsActive = isActive;
        State.LastModifiedAt = DateTime.UtcNow;

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<AccountMovementsState>> GetMovementsAsync(DateTime startDate, DateTime endDate)
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<AccountMovementsState>.Failure("Account not found"));

        var movements = _movements
            .Where(m => m.Date >= startDate && m.Date <= endDate)
            .OrderBy(m => m.Date)
            .ToList();

        // Calculate opening balance
        var openingBalance = 0m;
        var openingMovement = _movements
            .Where(m => m.Date < startDate)
            .OrderByDescending(m => m.Date)
            .FirstOrDefault();
        
        if (openingMovement != null)
            openingBalance = openingMovement.Balance;

        var totalDebits = movements.Sum(m => m.Debit);
        var totalCredits = movements.Sum(m => m.Credit);
        var closingBalance = movements.LastOrDefault()?.Balance ?? openingBalance;

        var result = new AccountMovementsState
        {
            Movements = movements,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits
        };

        return Task.FromResult(Result<AccountMovementsState>.Success(result));
    }

    private static bool IsDebitAccount(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Asset => true,
            AccountType.Expense => true,
            AccountType.ContraLiability => true,
            AccountType.ContraEquity => true,
            AccountType.ContraRevenue => true,
            _ => false
        };
    }
}