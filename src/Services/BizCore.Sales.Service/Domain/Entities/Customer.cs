using BizCore.Domain.Common;
using Ardalis.SmartEnum;

namespace BizCore.Sales.Domain.Entities;

public class Customer : AuditableEntity<Guid>, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string CustomerNumber { get; private set; }
    public string Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public CustomerType Type { get; private set; }
    public CustomerStatus Status { get; private set; }
    public string? TaxId { get; private set; }
    public string Currency { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public int PaymentTerms { get; private set; }
    public Guid? SalesRepId { get; private set; }
    
    private readonly List<CustomerAddress> _addresses = new();
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    private Customer() { }

    public Customer(
        Guid tenantId,
        string customerNumber,
        string name,
        CustomerType type,
        string currency)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        CustomerNumber = customerNumber;
        Name = name;
        Type = type;
        Currency = currency;
        Status = CustomerStatus.Active;
        PaymentTerms = 30;
        
        AddDomainEvent(new CustomerCreatedDomainEvent(Id, TenantId, CustomerNumber, Name));
    }

    public void UpdateBasicInfo(string name, string? email, string? phone, string? taxId)
    {
        Name = name;
        Email = email;
        Phone = phone;
        TaxId = taxId;
    }

    public void SetCreditLimit(decimal creditLimit)
    {
        CreditLimit = creditLimit;
    }

    public void UpdateBalance(decimal amount)
    {
        CurrentBalance += amount;
    }

    public void SetPaymentTerms(int days)
    {
        PaymentTerms = days;
    }

    public void AssignSalesRep(Guid salesRepId)
    {
        SalesRepId = salesRepId;
    }

    public void AddAddress(string type, string street, string city, string state, string postalCode, string country)
    {
        var address = new CustomerAddress(Id, type, street, city, state, postalCode, country);
        _addresses.Add(address);
    }

    public void Activate() => Status = CustomerStatus.Active;
    public void Deactivate() => Status = CustomerStatus.Inactive;
    public void Block() => Status = CustomerStatus.Blocked;
}

public class CustomerAddress : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string Type { get; private set; }
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public bool IsDefault { get; private set; }

    private CustomerAddress() { }

    public CustomerAddress(Guid customerId, string type, string street, string city, string state, string postalCode, string country)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Type = type;
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }
}

public class CustomerType : SmartEnum<CustomerType>
{
    public static readonly CustomerType Individual = new(1, nameof(Individual));
    public static readonly CustomerType Company = new(2, nameof(Company));
    public static readonly CustomerType Government = new(3, nameof(Government));

    private CustomerType(int value, string name) : base(name, value) { }
}

public class CustomerStatus : SmartEnum<CustomerStatus>
{
    public static readonly CustomerStatus Active = new(1, nameof(Active));
    public static readonly CustomerStatus Inactive = new(2, nameof(Inactive));
    public static readonly CustomerStatus Blocked = new(3, nameof(Blocked));

    private CustomerStatus(int value, string name) : base(name, value) { }
}

public record CustomerCreatedDomainEvent(
    Guid CustomerId,
    Guid TenantId,
    string CustomerNumber,
    string Name) : INotification;