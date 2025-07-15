using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Accounting.Domain.Entities;

public class Account : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AccountType Type { get; private set; }
    public string Currency { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public Account? ParentAccount { get; private set; }
    public int Level { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public bool AllowManualEntry { get; private set; }
    public bool RequiresCostCenter { get; private set; }
    public bool RequiresProject { get; private set; }
    
    private readonly List<Account> _childAccounts = new();
    public IReadOnlyCollection<Account> ChildAccounts => _childAccounts.AsReadOnly();

    private readonly List<AccountBalance> _balances = new();
    public IReadOnlyCollection<AccountBalance> Balances => _balances.AsReadOnly();

    private Account() { }

    public Account(
        Guid tenantId,
        string code,
        string name,
        AccountType type,
        string currency,
        Guid? parentAccountId = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Code = code;
        Name = name;
        Type = type;
        Currency = currency;
        ParentAccountId = parentAccountId;
        Level = CalculateLevel();
        IsActive = true;
        AllowManualEntry = true;
        
        AddDomainEvent(new AccountCreatedDomainEvent(Id, TenantId, Code, Name));
    }

    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetParentAccount(Guid? parentAccountId)
    {
        if (parentAccountId == Id)
            throw new BusinessRuleValidationException("Account cannot be its own parent");

        ParentAccountId = parentAccountId;
        Level = CalculateLevel();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void SetCostCenterRequirement(bool required) => RequiresCostCenter = required;
    public void SetProjectRequirement(bool required) => RequiresProject = required;

    private int CalculateLevel()
    {
        if (ParentAccountId == null) return 1;
        return ParentAccount?.Level + 1 ?? 2;
    }

    public AccountBalance GetOrCreateBalance(int year, int month)
    {
        var balance = _balances.FirstOrDefault(b => b.Year == year && b.Month == month);
        if (balance == null)
        {
            balance = new AccountBalance(Id, year, month, Currency);
            _balances.Add(balance);
        }
        return balance;
    }
}

public class AccountType : SmartEnum<AccountType>
{
    public static readonly AccountType Asset = new(1, nameof(Asset), "A", 1);
    public static readonly AccountType Liability = new(2, nameof(Liability), "L", -1);
    public static readonly AccountType Equity = new(3, nameof(Equity), "E", -1);
    public static readonly AccountType Revenue = new(4, nameof(Revenue), "R", -1);
    public static readonly AccountType Expense = new(5, nameof(Expense), "X", 1);

    public string Code { get; }
    public int NormalBalance { get; }

    private AccountType(int value, string name, string code, int normalBalance) : base(name, value)
    {
        Code = code;
        NormalBalance = normalBalance;
    }
}

public class AccountBalance : Entity<Guid>
{
    public Guid AccountId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public Money OpeningBalance { get; private set; }
    public Money Debits { get; private set; }
    public Money Credits { get; private set; }
    public Money ClosingBalance { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public bool IsClosed { get; private set; }

    private AccountBalance() { }

    public AccountBalance(Guid accountId, int year, int month, string currency)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Year = year;
        Month = month;
        OpeningBalance = Money.Zero(currency);
        Debits = Money.Zero(currency);
        Credits = Money.Zero(currency);
        ClosingBalance = Money.Zero(currency);
        LastUpdated = DateTime.UtcNow;
    }

    public void AddDebit(Money amount)
    {
        if (IsClosed)
            throw new BusinessRuleValidationException("Cannot modify closed period balance");

        Debits = Debits.Add(amount);
        RecalculateClosingBalance();
    }

    public void AddCredit(Money amount)
    {
        if (IsClosed)
            throw new BusinessRuleValidationException("Cannot modify closed period balance");

        Credits = Credits.Add(amount);
        RecalculateClosingBalance();
    }

    public void SetOpeningBalance(Money amount)
    {
        OpeningBalance = amount;
        RecalculateClosingBalance();
    }

    public void ClosePeriod()
    {
        IsClosed = true;
    }

    private void RecalculateClosingBalance()
    {
        ClosingBalance = OpeningBalance.Add(Debits).Subtract(Credits);
        LastUpdated = DateTime.UtcNow;
    }
}

public record AccountCreatedDomainEvent(
    Guid AccountId,
    Guid TenantId,
    string Code,
    string Name) : INotification;