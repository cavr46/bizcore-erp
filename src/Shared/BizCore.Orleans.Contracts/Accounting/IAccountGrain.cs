using Orleans;
using System;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Accounting;

public interface IAccountGrain : IEntityGrain<AccountState>, ITenantGrain
{
    Task<Result<Guid>> InitializeAsync(AccountInitCommand command);
    Task<Result> UpdateAsync(AccountUpdateCommand command);
    Task<Result<decimal>> GetBalanceAsync(DateTime? asOfDate = null);
    Task<Result> PostTransactionAsync(PostTransactionCommand command);
    Task<Result> SetActiveStatusAsync(bool isActive);
    Task<Result<AccountMovementsState>> GetMovementsAsync(DateTime startDate, DateTime endDate);
}

[GenerateSerializer]
public class AccountState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string AccountCode { get; set; } = string.Empty;
    [Id(3)] public string AccountName { get; set; } = string.Empty;
    [Id(4)] public AccountType AccountType { get; set; }
    [Id(5)] public string? ParentAccountCode { get; set; }
    [Id(6)] public int Level { get; set; }
    [Id(7)] public bool IsActive { get; set; }
    [Id(8)] public bool IsSystemAccount { get; set; }
    [Id(9)] public string CurrencyCode { get; set; } = string.Empty;
    [Id(10)] public decimal CurrentBalance { get; set; }
    [Id(11)] public DateTime CreatedAt { get; set; }
    [Id(12)] public DateTime? LastModifiedAt { get; set; }
    [Id(13)] public string? Description { get; set; }
    [Id(14)] public Dictionary<string, string> Metadata { get; set; } = new();
}

[GenerateSerializer]
public class AccountInitCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public string AccountCode { get; set; } = string.Empty;
    [Id(2)] public string AccountName { get; set; } = string.Empty;
    [Id(3)] public AccountType AccountType { get; set; }
    [Id(4)] public string? ParentAccountCode { get; set; }
    [Id(5)] public string CurrencyCode { get; set; } = "USD";
    [Id(6)] public string? Description { get; set; }
    [Id(7)] public bool IsSystemAccount { get; set; }
    [Id(8)] public string CreatedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class AccountUpdateCommand
{
    [Id(0)] public string AccountName { get; set; } = string.Empty;
    [Id(1)] public string? Description { get; set; }
    [Id(2)] public Dictionary<string, string>? Metadata { get; set; }
    [Id(3)] public string ModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class PostTransactionCommand
{
    [Id(0)] public Guid TransactionId { get; set; }
    [Id(1)] public decimal Amount { get; set; }
    [Id(2)] public TransactionType Type { get; set; }
    [Id(3)] public DateTime TransactionDate { get; set; }
    [Id(4)] public string Description { get; set; } = string.Empty;
    [Id(5)] public string Reference { get; set; } = string.Empty;
    [Id(6)] public string PostedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class AccountMovementsState
{
    [Id(0)] public List<AccountMovement> Movements { get; set; } = new();
    [Id(1)] public decimal OpeningBalance { get; set; }
    [Id(2)] public decimal ClosingBalance { get; set; }
    [Id(3)] public decimal TotalDebits { get; set; }
    [Id(4)] public decimal TotalCredits { get; set; }
}

[GenerateSerializer]
public class AccountMovement
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public DateTime Date { get; set; }
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public string Reference { get; set; } = string.Empty;
    [Id(4)] public decimal Debit { get; set; }
    [Id(5)] public decimal Credit { get; set; }
    [Id(6)] public decimal Balance { get; set; }
}

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5,
    ContraAsset = 6,
    ContraLiability = 7,
    ContraEquity = 8,
    ContraRevenue = 9,
    ContraExpense = 10
}

public enum TransactionType 
{
    Debit = 1,
    Credit = 2
}

[GenerateSerializer]
public class Result
{
    [Id(0)] public bool IsSuccess { get; set; }
    [Id(1)] public string? Error { get; set; }
    
    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

[GenerateSerializer]
public class Result<T> : Result
{
    [Id(2)] public T? Value { get; set; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public new static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}