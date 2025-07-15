using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Sales;
using BizCore.Orleans.Core.Base;

namespace BizCore.Sales.Service.Grains;

public class CustomerGrain : TenantGrainBase<CustomerState>, ICustomerGrain
{
    public CustomerGrain(
        [PersistentState("customer", "SalesStore")] IPersistentState<CustomerState> state)
        : base(state)
    {
    }

    public async Task<Result<Guid>> CreateAsync(CreateCustomerCommand command)
    {
        if (State.Id != Guid.Empty)
            return Result<Guid>.Failure("Customer already exists");

        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Guid>.Failure("Customer name is required");

        State.Id = this.GetPrimaryKey();
        State.TenantId = command.TenantId;
        State.CustomerCode = command.CustomerCode;
        State.Name = command.Name;
        State.LegalName = command.LegalName;
        State.TaxId = command.TaxId;
        State.Type = command.Type;
        State.Website = command.Website;
        State.Phone = command.Phone;
        State.Email = command.Email;
        State.BillingAddress = command.BillingAddress;
        State.ShippingAddress = command.ShippingAddress;
        State.CreditLimit = command.CreditLimit;
        State.CurrentBalance = 0;
        State.PaymentTerms = "Net30";
        State.DiscountPercentage = 0;
        State.IsActive = true;
        State.Contacts = new List<CustomerContact>();
        State.CreatedAt = DateTime.UtcNow;
        State.CustomFields = new Dictionary<string, string>();

        await WriteStateAsync();
        return Result<Guid>.Success(State.Id);
    }

    public async Task<Result> UpdateAsync(UpdateCustomerCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Customer not found");

        if (!string.IsNullOrWhiteSpace(command.Name))
            State.Name = command.Name;

        if (!string.IsNullOrWhiteSpace(command.LegalName))
            State.LegalName = command.LegalName;

        if (command.Website != null)
            State.Website = command.Website;

        if (command.Phone != null)
            State.Phone = command.Phone;

        if (command.Email != null)
            State.Email = command.Email;

        if (command.BillingAddress != null)
            State.BillingAddress = command.BillingAddress;

        if (command.ShippingAddress != null)
            State.ShippingAddress = command.ShippingAddress;

        if (command.DiscountPercentage.HasValue)
            State.DiscountPercentage = command.DiscountPercentage.Value;

        if (command.CustomFields != null)
        {
            foreach (var field in command.CustomFields)
            {
                State.CustomFields[field.Key] = field.Value;
            }
        }

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> SetCreditLimitAsync(decimal creditLimit, string approvedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Customer not found");

        if (creditLimit < 0)
            return Result.Failure("Credit limit cannot be negative");

        State.CreditLimit = creditLimit;
        State.CustomFields["CreditLimitApprovedBy"] = approvedBy;
        State.CustomFields["CreditLimitApprovedAt"] = DateTime.UtcNow.ToString("O");

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<CustomerCreditInfo>> GetCreditInfoAsync()
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<CustomerCreditInfo>.Failure("Customer not found"));

        var creditInfo = new CustomerCreditInfo
        {
            CreditLimit = State.CreditLimit,
            CurrentBalance = State.CurrentBalance,
            AvailableCredit = State.CreditLimit - State.CurrentBalance,
            IsOverLimit = State.CurrentBalance > State.CreditLimit,
            OverdueDays = 0, // Would be calculated from actual invoices
            OldestInvoiceDate = State.LastTransactionDate
        };

        return Task.FromResult(Result<CustomerCreditInfo>.Success(creditInfo));
    }

    public async Task<Result> AddContactAsync(CustomerContact contact)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Customer not found");

        if (string.IsNullOrWhiteSpace(contact.Name))
            return Result.Failure("Contact name is required");

        // If this is being set as primary, unset other primary contacts
        if (contact.IsPrimary)
        {
            foreach (var existingContact in State.Contacts)
            {
                existingContact.IsPrimary = false;
            }
        }

        contact.Id = Guid.NewGuid();
        State.Contacts.Add(contact);

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> RemoveContactAsync(Guid contactId)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Customer not found");

        var contact = State.Contacts.FirstOrDefault(c => c.Id == contactId);
        if (contact == null)
            return Result.Failure("Contact not found");

        State.Contacts.Remove(contact);

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> SetPaymentTermsAsync(string paymentTerms)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Customer not found");

        if (string.IsNullOrWhiteSpace(paymentTerms))
            return Result.Failure("Payment terms cannot be empty");

        State.PaymentTerms = paymentTerms;

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<List<CustomerTransaction>>> GetRecentTransactionsAsync(int count = 10)
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<List<CustomerTransaction>>.Failure("Customer not found"));

        // In a real implementation, this would query actual transaction history
        // For now, return empty list
        var transactions = new List<CustomerTransaction>();

        return Task.FromResult(Result<List<CustomerTransaction>>.Success(transactions));
    }
}