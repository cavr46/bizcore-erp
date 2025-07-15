using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizCore.Orleans.Contracts.Base;

namespace BizCore.Orleans.Contracts.Purchasing;

public interface ISupplierGrain : IEntityGrain<SupplierState>, ITenantGrain
{
    Task<Result<Guid>> CreateAsync(CreateSupplierCommand command);
    Task<Result> UpdateAsync(UpdateSupplierCommand command);
    Task<Result> SetActiveStatusAsync(bool isActive);
    Task<Result> AddContactAsync(SupplierContact contact);
    Task<Result> RemoveContactAsync(Guid contactId);
    Task<Result> UpdateRatingAsync(decimal rating, string reviewedBy);
    Task<Result<SupplierPerformance>> GetPerformanceAsync();
    Task<Result> SetPaymentTermsAsync(string paymentTerms);
    Task<Result<List<SupplierDocument>>> GetDocumentsAsync();
    Task<Result> AddDocumentAsync(SupplierDocument document);
}

[GenerateSerializer]
public class SupplierState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid TenantId { get; set; }
    [Id(2)] public string SupplierCode { get; set; } = string.Empty;
    [Id(3)] public string Name { get; set; } = string.Empty;
    [Id(4)] public string LegalName { get; set; } = string.Empty;
    [Id(5)] public string TaxId { get; set; } = string.Empty;
    [Id(6)] public SupplierType Type { get; set; }
    [Id(7)] public string? Website { get; set; }
    [Id(8)] public string? Phone { get; set; }
    [Id(9)] public string? Email { get; set; }
    [Id(10)] public Address? Address { get; set; }
    [Id(11)] public string PaymentTerms { get; set; } = "Net30";
    [Id(12)] public decimal Rating { get; set; }
    [Id(13)] public string CurrencyCode { get; set; } = "USD";
    [Id(14)] public bool IsActive { get; set; } = true;
    [Id(15)] public bool IsApproved { get; set; }
    [Id(16)] public List<SupplierContact> Contacts { get; set; } = new();
    [Id(17)] public List<SupplierDocument> Documents { get; set; } = new();
    [Id(18)] public DateTime CreatedAt { get; set; }
    [Id(19)] public DateTime? LastTransactionDate { get; set; }
    [Id(20)] public Dictionary<string, string> CustomFields { get; set; } = new();
}

[GenerateSerializer]
public class SupplierContact
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string? Title { get; set; }
    [Id(3)] public string? Email { get; set; }
    [Id(4)] public string? Phone { get; set; }
    [Id(5)] public string? Mobile { get; set; }
    [Id(6)] public bool IsPrimary { get; set; }
    [Id(7)] public ContactType Type { get; set; }
    [Id(8)] public string? Department { get; set; }
}

[GenerateSerializer]
public class SupplierDocument
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string DocumentType { get; set; } = string.Empty;
    [Id(3)] public string FileUrl { get; set; } = string.Empty;
    [Id(4)] public DateTime UploadedAt { get; set; }
    [Id(5)] public string UploadedBy { get; set; } = string.Empty;
    [Id(6)] public DateTime? ExpiresAt { get; set; }
    [Id(7)] public bool IsRequired { get; set; }
    [Id(8)] public bool IsApproved { get; set; }
}

[GenerateSerializer]
public class CreateSupplierCommand
{
    [Id(0)] public Guid TenantId { get; set; }
    [Id(1)] public string SupplierCode { get; set; } = string.Empty;
    [Id(2)] public string Name { get; set; } = string.Empty;
    [Id(3)] public string LegalName { get; set; } = string.Empty;
    [Id(4)] public string TaxId { get; set; } = string.Empty;
    [Id(5)] public SupplierType Type { get; set; }
    [Id(6)] public string? Website { get; set; }
    [Id(7)] public string? Phone { get; set; }
    [Id(8)] public string? Email { get; set; }
    [Id(9)] public Address? Address { get; set; }
    [Id(10)] public string PaymentTerms { get; set; } = "Net30";
    [Id(11)] public string CurrencyCode { get; set; } = "USD";
    [Id(12)] public string CreatedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class UpdateSupplierCommand
{
    [Id(0)] public string? Name { get; set; }
    [Id(1)] public string? LegalName { get; set; }
    [Id(2)] public string? Website { get; set; }
    [Id(3)] public string? Phone { get; set; }
    [Id(4)] public string? Email { get; set; }
    [Id(5)] public Address? Address { get; set; }
    [Id(6)] public string? PaymentTerms { get; set; }
    [Id(7)] public Dictionary<string, string>? CustomFields { get; set; }
    [Id(8)] public string ModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SupplierPerformance
{
    [Id(0)] public decimal AverageRating { get; set; }
    [Id(1)] public decimal OnTimeDeliveryRate { get; set; }
    [Id(2)] public decimal QualityRating { get; set; }
    [Id(3)] public int TotalOrders { get; set; }
    [Id(4)] public decimal TotalSpent { get; set; }
    [Id(5)] public int DaysPaymentTerm { get; set; }
    [Id(6)] public DateTime? LastOrderDate { get; set; }
}

public enum SupplierType
{
    Manufacturer = 1,
    Wholesaler = 2,
    Distributor = 3,
    ServiceProvider = 4,
    Consultant = 5,
    Contractor = 6,
    Other = 99
}

public enum ContactType
{
    Primary = 1,
    Billing = 2,
    Technical = 3,
    Sales = 4,
    Support = 5,
    Logistics = 6,
    Quality = 7,
    Other = 99
}