using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Purchasing;
using BizCore.Orleans.Core.Base;

namespace BizCore.Purchasing.Service.Grains;

public class SupplierGrain : TenantGrainBase<SupplierState>, ISupplierGrain
{
    public SupplierGrain(
        [PersistentState("supplier", "PurchasingStore")] IPersistentState<SupplierState> state)
        : base(state)
    {
    }

    public async Task<Result<Guid>> CreateAsync(CreateSupplierCommand command)
    {
        if (State.Id != Guid.Empty)
            return Result<Guid>.Failure("Supplier already exists");

        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Guid>.Failure("Supplier name is required");

        if (string.IsNullOrWhiteSpace(command.SupplierCode))
            return Result<Guid>.Failure("Supplier code is required");

        State.Id = this.GetPrimaryKey();
        State.TenantId = command.TenantId;
        State.SupplierCode = command.SupplierCode;
        State.Name = command.Name;
        State.LegalName = command.LegalName;
        State.TaxId = command.TaxId;
        State.Type = command.Type;
        State.Website = command.Website;
        State.Phone = command.Phone;
        State.Email = command.Email;
        State.Address = command.Address;
        State.PaymentTerms = command.PaymentTerms;
        State.Rating = 0;
        State.CurrencyCode = command.CurrencyCode;
        State.IsActive = true;
        State.IsApproved = false;
        State.Contacts = new List<SupplierContact>();
        State.Documents = new List<SupplierDocument>();
        State.CreatedAt = DateTime.UtcNow;
        State.CustomFields = new Dictionary<string, string>();

        await WriteStateAsync();
        return Result<Guid>.Success(State.Id);
    }

    public async Task<Result> UpdateAsync(UpdateSupplierCommand command)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Supplier not found");

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

        if (command.Address != null)
            State.Address = command.Address;

        if (command.PaymentTerms != null)
            State.PaymentTerms = command.PaymentTerms;

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

    public async Task<Result> SetActiveStatusAsync(bool isActive)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Supplier not found");

        State.IsActive = isActive;

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> AddContactAsync(SupplierContact contact)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Supplier not found");

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
            return Result.Failure("Supplier not found");

        var contact = State.Contacts.FirstOrDefault(c => c.Id == contactId);
        if (contact == null)
            return Result.Failure("Contact not found");

        State.Contacts.Remove(contact);

        await WriteStateAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateRatingAsync(decimal rating, string reviewedBy)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Supplier not found");

        if (rating < 0 || rating > 5)
            return Result.Failure("Rating must be between 0 and 5");

        State.Rating = rating;
        State.CustomFields["RatingReviewedBy"] = reviewedBy;
        State.CustomFields["RatingReviewedAt"] = DateTime.UtcNow.ToString("O");

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<SupplierPerformance>> GetPerformanceAsync()
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<SupplierPerformance>.Failure("Supplier not found"));

        // In a real implementation, this would calculate performance metrics
        // from actual purchase orders and receipts
        var performance = new SupplierPerformance
        {
            AverageRating = State.Rating,
            OnTimeDeliveryRate = 0.95m, // 95% on-time delivery
            QualityRating = 4.2m,
            TotalOrders = 0,
            TotalSpent = 0,
            DaysPaymentTerm = int.TryParse(State.PaymentTerms.Replace("Net", ""), out var days) ? days : 30,
            LastOrderDate = State.LastTransactionDate
        };

        return Task.FromResult(Result<SupplierPerformance>.Success(performance));
    }

    public async Task<Result> SetPaymentTermsAsync(string paymentTerms)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Supplier not found");

        if (string.IsNullOrWhiteSpace(paymentTerms))
            return Result.Failure("Payment terms cannot be empty");

        State.PaymentTerms = paymentTerms;

        await WriteStateAsync();
        return Result.Success();
    }

    public Task<Result<List<SupplierDocument>>> GetDocumentsAsync()
    {
        if (State.Id == Guid.Empty)
            return Task.FromResult(Result<List<SupplierDocument>>.Failure("Supplier not found"));

        return Task.FromResult(Result<List<SupplierDocument>>.Success(State.Documents));
    }

    public async Task<Result> AddDocumentAsync(SupplierDocument document)
    {
        if (State.Id == Guid.Empty)
            return Result.Failure("Supplier not found");

        if (string.IsNullOrWhiteSpace(document.Name))
            return Result.Failure("Document name is required");

        if (string.IsNullOrWhiteSpace(document.FileUrl))
            return Result.Failure("Document file URL is required");

        document.Id = Guid.NewGuid();
        document.UploadedAt = DateTime.UtcNow;
        State.Documents.Add(document);

        await WriteStateAsync();
        return Result.Success();
    }
}