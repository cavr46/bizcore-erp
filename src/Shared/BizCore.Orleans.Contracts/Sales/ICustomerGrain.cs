using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Sales;

public interface ICustomerGrain : IEntityGrain<CustomerState>, ITenantGrain
{
    Task<Result<Guid>> CreateAsync(CreateCustomerCommand command);
    Task<Result> UpdateAsync(UpdateCustomerCommand command);
    Task<Result> SetCreditLimitAsync(decimal creditLimit, string approvedBy);
    Task<Result<CustomerCreditInfo>> GetCreditInfoAsync();
    Task<Result> AddContactAsync(CustomerContact contact);
    Task<Result> RemoveContactAsync(Guid contactId);
    Task<Result> SetPaymentTermsAsync(string paymentTerms);
    Task<Result<List<CustomerTransaction>>> GetRecentTransactionsAsync(int count = 10);
}

[GenerateSerializer]
public class CustomerState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string CustomerCode { get; set; } = string.Empty;
    [Id(3)] public string Name { get; set; } = string.Empty;
    [Id(4)] public string LegalName { get; set; } = string.Empty;
    [Id(5)] public string TaxId { get; set; } = string.Empty;
    [Id(6)] public CustomerType Type { get; set; }
    [Id(7)] public string? Website { get; set; }
    [Id(8)] public string? Phone { get; set; }
    [Id(9)] public string? Email { get; set; }
    [Id(10)] public Address? BillingAddress { get; set; }
    [Id(11)] public Address? ShippingAddress { get; set; }
    [Id(12)] public decimal CreditLimit { get; set; }
    [Id(13)] public decimal CurrentBalance { get; set; }
    [Id(14)] public string PaymentTerms { get; set; } = "Net30";
    [Id(15)] public string? PriceListCode { get; set; }
    [Id(16)] public decimal DiscountPercentage { get; set; }
    [Id(17)] public bool IsActive { get; set; }
    [Id(18)] public List<CustomerContact> Contacts { get; set; } = new();
    [Id(19)] public DateTime CreatedAt { get; set; }
    [Id(20)] public DateTime? LastTransactionDate { get; set; }
    [Id(21)] public Dictionary<string, string> CustomFields { get; set; } = new();
}

[GenerateSerializer]
public class CustomerContact
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string? Title { get; set; }
    [Id(3)] public string? Email { get; set; }
    [Id(4)] public string? Phone { get; set; }
    [Id(5)] public string? Mobile { get; set; }
    [Id(6)] public bool IsPrimary { get; set; }
    [Id(7)] public ContactType Type { get; set; }
}

[GenerateSerializer]
public class Address
{
    [Id(0)] public string Street1 { get; set; } = string.Empty;
    [Id(1)] public string? Street2 { get; set; }
    [Id(2)] public string City { get; set; } = string.Empty;
    [Id(3)] public string StateProvince { get; set; } = string.Empty;
    [Id(4)] public string PostalCode { get; set; } = string.Empty;
    [Id(5)] public string Country { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CreateCustomerCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public string CustomerCode { get; set; } = string.Empty;
    [Id(2)] public string Name { get; set; } = string.Empty;
    [Id(3)] public string LegalName { get; set; } = string.Empty;
    [Id(4)] public string TaxId { get; set; } = string.Empty;
    [Id(5)] public CustomerType Type { get; set; }
    [Id(6)] public string? Website { get; set; }
    [Id(7)] public string? Phone { get; set; }
    [Id(8)] public string? Email { get; set; }
    [Id(9)] public Address? BillingAddress { get; set; }
    [Id(10)] public Address? ShippingAddress { get; set; }
    [Id(11)] public decimal CreditLimit { get; set; }
    [Id(12)] public string CreatedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UpdateCustomerCommand
{
    [Id(0)] public string? Name { get; set; }
    [Id(1)] public string? LegalName { get; set; }
    [Id(2)] public string? Website { get; set; }
    [Id(3)] public string? Phone { get; set; }
    [Id(4)] public string? Email { get; set; }
    [Id(5)] public Address? BillingAddress { get; set; }
    [Id(6)] public Address? ShippingAddress { get; set; }
    [Id(7)] public decimal? DiscountPercentage { get; set; }
    [Id(8)] public Dictionary<string, string>? CustomFields { get; set; }
    [Id(9)] public string ModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CustomerCreditInfo
{
    [Id(0)] public decimal CreditLimit { get; set; }
    [Id(1)] public decimal CurrentBalance { get; set; }
    [Id(2)] public decimal AvailableCredit { get; set; }
    [Id(3)] public bool IsOverLimit { get; set; }
    [Id(4)] public int OverdueDays { get; set; }
    [Id(5)] public DateTime? OldestInvoiceDate { get; set; }
}

[GenerateSerializer]
public class CustomerTransaction
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public DateTime Date { get; set; }
    [Id(2)] public string Type { get; set; } = string.Empty;
    [Id(3)] public string DocumentNumber { get; set; } = string.Empty;
    [Id(4)] public decimal Amount { get; set; }
    [Id(5)] public decimal Balance { get; set; }
}

public enum CustomerType
{
    Individual = 1,
    Company = 2,
    Government = 3,
    NonProfit = 4
}

public enum ContactType
{
    Primary = 1,
    Billing = 2,
    Shipping = 3,
    Sales = 4,
    Support = 5,
    Other = 99
}