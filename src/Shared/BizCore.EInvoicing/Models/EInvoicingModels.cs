using System.ComponentModel.DataAnnotations;

namespace BizCore.EInvoicing.Models;

/// <summary>
/// Electronic invoice model with multi-country support
/// </summary>
public class ElectronicInvoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Number { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public InvoiceType Type { get; set; } = InvoiceType.Sale;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1.0m;
    
    // Parties
    public InvoiceParty Issuer { get; set; } = new();
    public InvoiceParty Customer { get; set; } = new();
    public InvoiceParty? ShipTo { get; set; }
    public InvoiceParty? BillTo { get; set; }
    
    // Lines
    public List<InvoiceLine> Lines { get; set; } = new();
    
    // Totals
    public InvoiceTotals Totals { get; set; } = new();
    
    // Taxes
    public List<TaxTotal> TaxTotals { get; set; } = new();
    
    // Legal requirements
    public InvoiceLegalData LegalData { get; set; } = new();
    
    // Electronic signature
    public DigitalSignature? Signature { get; set; }
    
    // Country-specific data
    public Dictionary<string, object> CountrySpecificData { get; set; } = new();
    
    // Processing data
    public InvoiceProcessingData ProcessingData { get; set; } = new();
    
    // Documents
    public List<InvoiceDocument> Attachments { get; set; } = new();
    
    // References
    public List<DocumentReference> References { get; set; } = new();
    
    // Payment information
    public List<PaymentInformation> PaymentInformation { get; set; } = new();
    
    // Delivery information
    public DeliveryInformation? DeliveryInformation { get; set; }
    
    // Additional information
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Invoice types
/// </summary>
public enum InvoiceType
{
    Sale,
    Purchase,
    CreditNote,
    DebitNote,
    Proforma,
    Commercial,
    Export,
    Import,
    Service,
    Recurring,
    Adjustment,
    Cancellation
}

/// <summary>
/// Invoice status
/// </summary>
public enum InvoiceStatus
{
    Draft,
    Pending,
    Submitted,
    Approved,
    Rejected,
    Signed,
    Sent,
    Delivered,
    Paid,
    Cancelled,
    Voided,
    Error
}

/// <summary>
/// Invoice party (issuer, customer, etc.)
/// </summary>
public class InvoiceParty
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public PartyType Type { get; set; } = PartyType.Company;
    
    // Identifiers
    public List<PartyIdentifier> Identifiers { get; set; } = new();
    
    // Address
    public Address Address { get; set; } = new();
    
    // Contact
    public ContactInformation Contact { get; set; } = new();
    
    // Tax information
    public TaxInformation TaxInfo { get; set; } = new();
    
    // Bank information
    public List<BankAccount> BankAccounts { get; set; } = new();
    
    // Legal
    public string RegistrationCountry { get; set; } = string.Empty;
    public DateTime? RegistrationDate { get; set; }
    public string LegalForm { get; set; } = string.Empty;
    
    // Additional data
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Party types
/// </summary>
public enum PartyType
{
    Company,
    Individual,
    Government,
    NonProfit,
    Branch,
    Subsidiary
}

/// <summary>
/// Party identifier (tax ID, registration number, etc.)
/// </summary>
public class PartyIdentifier
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsPrimary { get; set; } = false;
}

/// <summary>
/// Address information
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string AdditionalStreet { get; set; } = string.Empty;
    public string BuildingNumber { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Apartment { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Dictionary<string, object> AdditionalFields { get; set; } = new();
}

/// <summary>
/// Contact information
/// </summary>
public class ContactInformation
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> AlternativeEmails { get; set; } = new();
    public List<string> AlternativePhones { get; set; } = new();
}

/// <summary>
/// Tax information
/// </summary>
public class TaxInformation
{
    public string TaxId { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string TaxScheme { get; set; } = string.Empty;
    public string TaxCategory { get; set; } = string.Empty;
    public bool IsVatExempt { get; set; } = false;
    public string VatExemptionReason { get; set; } = string.Empty;
    public List<TaxRegistration> TaxRegistrations { get; set; } = new();
    public TaxResidency TaxResidency { get; set; } = new();
}

/// <summary>
/// Tax registration
/// </summary>
public class TaxRegistration
{
    public string TaxType { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public DateTime? RegistrationDate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Tax residency information
/// </summary>
public class TaxResidency
{
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string TaxResidencyNumber { get; set; } = string.Empty;
    public bool IsTaxResident { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// Bank account information
/// </summary>
public class BankAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Invoice line item
/// </summary>
public class InvoiceLine
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public LineType Type { get; set; } = LineType.Item;
    
    // Quantity and units
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "EA";
    public string UnitCode { get; set; } = string.Empty;
    
    // Pricing
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal NetAmount { get; set; }
    
    // Taxes
    public List<LineTax> Taxes { get; set; } = new();
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    
    // Item details
    public ItemDetails? ItemDetails { get; set; }
    
    // References
    public List<LineReference> References { get; set; } = new();
    
    // Delivery
    public DateTime? DeliveryDate { get; set; }
    public string DeliveryLocation { get; set; } = string.Empty;
    
    // Additional data
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Line types
/// </summary>
public enum LineType
{
    Item,
    Service,
    Charge,
    Allowance,
    Tax,
    Discount,
    Shipping,
    Insurance,
    Other
}

/// <summary>
/// Line tax information
/// </summary>
public class LineTax
{
    public string TaxType { get; set; } = string.Empty;
    public string TaxCategory { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string TaxScheme { get; set; } = string.Empty;
    public string ExemptionReason { get; set; } = string.Empty;
    public bool IsExempt { get; set; } = false;
}

/// <summary>
/// Item details
/// </summary>
public class ItemDetails
{
    public string Manufacturer { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string OriginCountry { get; set; } = string.Empty;
    public List<ItemClassification> Classifications { get; set; } = new();
    public ItemMeasurements? Measurements { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Item classification (HS codes, etc.)
/// </summary>
public class ItemClassification
{
    public string ClassificationScheme { get; set; } = string.Empty;
    public string ClassificationCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Item measurements
/// </summary>
public class ItemMeasurements
{
    public decimal? Weight { get; set; }
    public string WeightUnit { get; set; } = string.Empty;
    public decimal? Volume { get; set; }
    public string VolumeUnit { get; set; } = string.Empty;
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string DimensionUnit { get; set; } = string.Empty;
}

/// <summary>
/// Line reference (to other documents)
/// </summary>
public class LineReference
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public int? LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Invoice totals
/// </summary>
public class InvoiceTotals
{
    public decimal LineExtensionAmount { get; set; }
    public decimal TaxExclusiveAmount { get; set; }
    public decimal TaxInclusiveAmount { get; set; }
    public decimal AllowanceTotalAmount { get; set; }
    public decimal ChargeTotalAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public RoundingRule RoundingRule { get; set; } = new();
}

/// <summary>
/// Rounding rule
/// </summary>
public class RoundingRule
{
    public int DecimalPlaces { get; set; } = 2;
    public RoundingMode Mode { get; set; } = RoundingMode.HalfUp;
    public bool ApplyToLineAmounts { get; set; } = true;
    public bool ApplyToTaxAmounts { get; set; } = true;
    public bool ApplyToTotals { get; set; } = true;
}

/// <summary>
/// Rounding modes
/// </summary>
public enum RoundingMode
{
    Up,
    Down,
    HalfUp,
    HalfDown,
    HalfEven,
    Banker
}

/// <summary>
/// Tax total
/// </summary>
public class TaxTotal
{
    public decimal TaxAmount { get; set; }
    public string TaxCurrency { get; set; } = string.Empty;
    public List<TaxSubtotal> TaxSubtotals { get; set; } = new();
}

/// <summary>
/// Tax subtotal
/// </summary>
public class TaxSubtotal
{
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public TaxCategory TaxCategory { get; set; } = new();
    public decimal Percent { get; set; }
    public string BaseUnitMeasure { get; set; } = string.Empty;
    public decimal PerUnitAmount { get; set; }
}

/// <summary>
/// Tax category
/// </summary>
public class TaxCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public TaxScheme TaxScheme { get; set; } = new();
    public string ExemptionReasonCode { get; set; } = string.Empty;
    public string ExemptionReason { get; set; } = string.Empty;
}

/// <summary>
/// Tax scheme
/// </summary>
public class TaxScheme
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string JurisdictionRegionAddress { get; set; } = string.Empty;
    public string TaxTypeCode { get; set; } = string.Empty;
}

/// <summary>
/// Invoice legal data
/// </summary>
public class InvoiceLegalData
{
    public string LegalMonetaryTotal { get; set; } = string.Empty;
    public string InvoiceTypeCode { get; set; } = string.Empty;
    public string DocumentCurrencyCode { get; set; } = string.Empty;
    public string TaxCurrencyCode { get; set; } = string.Empty;
    public string InvoicePeriod { get; set; } = string.Empty;
    public List<string> LegalReferences { get; set; } = new();
    public List<LegalNote> LegalNotes { get; set; } = new();
    public ComplianceData ComplianceData { get; set; } = new();
}

/// <summary>
/// Legal note
/// </summary>
public class LegalNote
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
}

/// <summary>
/// Compliance data for different countries
/// </summary>
public class ComplianceData
{
    public string Country { get; set; } = string.Empty;
    public string Regime { get; set; } = string.Empty;
    public Dictionary<string, object> CountrySpecificFields { get; set; } = new();
    public List<ComplianceRequirement> Requirements { get; set; } = new();
    public ComplianceStatus Status { get; set; } = new();
}

/// <summary>
/// Compliance requirement
/// </summary>
public class ComplianceRequirement
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
    public bool IsCompliant { get; set; } = false;
    public string NonComplianceReason { get; set; } = string.Empty;
    public DateTime? ComplianceDate { get; set; }
}

/// <summary>
/// Compliance status
/// </summary>
public class ComplianceStatus
{
    public bool IsCompliant { get; set; } = false;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastChecked { get; set; }
    public List<string> Violations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Digital signature
/// </summary>
public class DigitalSignature
{
    public string SignatureId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string SignatureValue { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public string SignedBy { get; set; } = string.Empty;
    public CertificateInfo Certificate { get; set; } = new();
    public string TimestampToken { get; set; } = string.Empty;
    public SignatureValidation Validation { get; set; } = new();
}

/// <summary>
/// Certificate information
/// </summary>
public class CertificateInfo
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string Thumbprint { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public List<string> KeyUsage { get; set; } = new();
}

/// <summary>
/// Signature validation
/// </summary>
public class SignatureValidation
{
    public bool IsValid { get; set; } = false;
    public DateTime? ValidatedAt { get; set; }
    public string ValidationResult { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public string ValidatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Invoice processing data
/// </summary>
public class InvoiceProcessingData
{
    public string SubmissionId { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string SubmissionStatus { get; set; } = string.Empty;
    public string ApprovalCode { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public List<ProcessingError> Errors { get; set; } = new();
    public List<ProcessingWarning> Warnings { get; set; } = new();
    public ProcessingTracking Tracking { get; set; } = new();
    public AuditTrail AuditTrail { get; set; } = new();
}

/// <summary>
/// Processing error
/// </summary>
public class ProcessingError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Processing warning
/// </summary>
public class ProcessingWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Processing tracking
/// </summary>
public class ProcessingTracking
{
    public List<ProcessingStep> Steps { get; set; } = new();
    public string CurrentStep { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; } = 0;
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Processing step
/// </summary>
public class ProcessingStep
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProcessingStepStatus Status { get; set; } = ProcessingStepStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Result { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Processing step status
/// </summary>
public enum ProcessingStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Cancelled
}

/// <summary>
/// Audit trail
/// </summary>
public class AuditTrail
{
    public List<AuditEntry> Entries { get; set; } = new();
}

/// <summary>
/// Audit entry
/// </summary>
public class AuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public string IPAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Invoice document (attachment)
/// </summary>
public class InvoiceDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public DocumentType Type { get; set; } = DocumentType.Attachment;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = string.Empty;
}

/// <summary>
/// Document types
/// </summary>
public enum DocumentType
{
    Attachment,
    PDF,
    XML,
    Image,
    Certificate,
    Contract,
    Receipt,
    Other
}

/// <summary>
/// Document reference
/// </summary>
public class DocumentReference
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public string IssuerParty { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string URI { get; set; } = string.Empty;
}

/// <summary>
/// Payment information
/// </summary>
public class PaymentInformation
{
    public string PaymentId { get; set; } = string.Empty;
    public PaymentMeans PaymentMeans { get; set; } = new();
    public PaymentTerms PaymentTerms { get; set; } = new();
    public List<PaymentInstruction> Instructions { get; set; } = new();
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Reference { get; set; } = string.Empty;
}

/// <summary>
/// Payment means
/// </summary>
public class PaymentMeans
{
    public string PaymentMeansCode { get; set; } = string.Empty;
    public string PaymentMeansText { get; set; } = string.Empty;
    public DateTime? PaymentDueDate { get; set; }
    public string PaymentChannelCode { get; set; } = string.Empty;
    public PayeeFinancialAccount? PayeeFinancialAccount { get; set; }
    public CardAccount? CardAccount { get; set; }
    public PaymentMandate? PaymentMandate { get; set; }
}

/// <summary>
/// Payee financial account
/// </summary>
public class PayeeFinancialAccount
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountTypeCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public FinancialInstitutionBranch? FinancialInstitutionBranch { get; set; }
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Financial institution branch
/// </summary>
public class FinancialInstitutionBranch
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FinancialInstitution? FinancialInstitution { get; set; }
    public Address? Address { get; set; }
}

/// <summary>
/// Financial institution
/// </summary>
public class FinancialInstitution
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

/// <summary>
/// Card account
/// </summary>
public class CardAccount
{
    public string PrimaryAccountNumberId { get; set; } = string.Empty;
    public string CardTypeCode { get; set; } = string.Empty;
    public string ValidityStartDate { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string IssuerNumberId { get; set; } = string.Empty;
    public string IssueNumberId { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
}

/// <summary>
/// Payment mandate
/// </summary>
public class PaymentMandate
{
    public string Id { get; set; } = string.Empty;
    public string MandateTypeCode { get; set; } = string.Empty;
    public decimal? MaximumAmount { get; set; }
    public DateTime? ValidityPeriodStartDate { get; set; }
    public DateTime? ValidityPeriodEndDate { get; set; }
    public InvoiceParty? PayerParty { get; set; }
    public PayeeFinancialAccount? PayerFinancialAccount { get; set; }
}

/// <summary>
/// Payment terms
/// </summary>
public class PaymentTerms
{
    public string Id { get; set; } = string.Empty;
    public string PaymentTermsText { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string InstallmentDueDate { get; set; } = string.Empty;
    public decimal? PenaltyAmount { get; set; }
    public decimal? PenaltyPercentage { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public SettlementDiscount? SettlementDiscount { get; set; }
}

/// <summary>
/// Settlement discount
/// </summary>
public class SettlementDiscount
{
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public DateTime? CalculationDate { get; set; }
    public DateTime? LastDate { get; set; }
}

/// <summary>
/// Payment instruction
/// </summary>
public class PaymentInstruction
{
    public string InstructionType { get; set; } = string.Empty;
    public string InstructionText { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Delivery information
/// </summary>
public class DeliveryInformation
{
    public DateTime? ActualDeliveryDate { get; set; }
    public DateTime? LatestDeliveryDate { get; set; }
    public Address? DeliveryLocation { get; set; }
    public InvoiceParty? DeliveryParty { get; set; }
    public List<DeliveryTerms> DeliveryTerms { get; set; } = new();
    public Shipment? Shipment { get; set; }
}

/// <summary>
/// Delivery terms
/// </summary>
public class DeliveryTerms
{
    public string Id { get; set; } = string.Empty;
    public string SpecialTerms { get; set; } = string.Empty;
    public decimal? LossRiskResponsibilityCode { get; set; }
    public string LossRisk { get; set; } = string.Empty;
    public Address? DeliveryLocation { get; set; }
    public AllowanceCharge? AllowanceCharge { get; set; }
}

/// <summary>
/// Allowance charge
/// </summary>
public class AllowanceCharge
{
    public bool ChargeIndicator { get; set; }
    public string AllowanceChargeReasonCode { get; set; } = string.Empty;
    public string AllowanceChargeReason { get; set; } = string.Empty;
    public decimal? MultiplierFactorNumeric { get; set; }
    public bool PrepaidIndicator { get; set; }
    public decimal SequenceNumeric { get; set; }
    public decimal Amount { get; set; }
    public decimal? BaseAmount { get; set; }
    public string AccountingCostCode { get; set; } = string.Empty;
    public string AccountingCost { get; set; } = string.Empty;
    public List<TaxCategory> TaxCategories { get; set; } = new();
}

/// <summary>
/// Shipment information
/// </summary>
public class Shipment
{
    public string Id { get; set; } = string.Empty;
    public string HandlingCode { get; set; } = string.Empty;
    public string HandlingInstructions { get; set; } = string.Empty;
    public string Information { get; set; } = string.Empty;
    public decimal? GrossWeightMeasure { get; set; }
    public decimal? NetWeightMeasure { get; set; }
    public decimal? NetNetWeightMeasure { get; set; }
    public decimal? GrossVolumeMeasure { get; set; }
    public decimal? NetVolumeMeasure { get; set; }
    public int? TotalGoodsItemQuantity { get; set; }
    public int? TotalTransportHandlingUnitQuantity { get; set; }
    public bool? InsuranceValueIndicator { get; set; }
    public bool? DeclaredCustomsValueIndicator { get; set; }
    public bool? DeclaredForCarriageValueIndicator { get; set; }
    public bool? DeclaredStatisticsValueIndicator { get; set; }
    public bool? FreeOnBoardValueIndicator { get; set; }
    public List<string> SpecialInstructions { get; set; } = new();
    public Consignment? Consignment { get; set; }
    public List<GoodsItem> GoodsItems { get; set; } = new();
    public List<ShipmentStage> ShipmentStages { get; set; } = new();
}

/// <summary>
/// Consignment
/// </summary>
public class Consignment
{
    public string Id { get; set; } = string.Empty;
    public string CarrierAssignedId { get; set; } = string.Empty;
    public string ConsigneeAssignedId { get; set; } = string.Empty;
    public string ConsignorAssignedId { get; set; } = string.Empty;
    public string FreightForwarderAssignedId { get; set; } = string.Empty;
    public string BrokerAssignedId { get; set; } = string.Empty;
    public string ContractedCarrierAssignedId { get; set; } = string.Empty;
    public string PerformingCarrierAssignedId { get; set; } = string.Empty;
    public string Information { get; set; } = string.Empty;
    public decimal? GrossWeightMeasure { get; set; }
    public decimal? NetWeightMeasure { get; set; }
    public decimal? GrossVolumeMeasure { get; set; }
    public decimal? NetVolumeMeasure { get; set; }
    public decimal? LoadingLengthMeasure { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public bool? HazardousRiskIndicator { get; set; }
    public bool? AnimalFoodIndicator { get; set; }
    public bool? HumanFoodIndicator { get; set; }
    public bool? LivestockIndicator { get; set; }
    public bool? BulkCargoIndicator { get; set; }
    public bool? ContainerizedIndicator { get; set; }
    public bool? GeneralCargoIndicator { get; set; }
    public bool? SpecialSecurityIndicator { get; set; }
    public bool? ThirdPartyPayerIndicator { get; set; }
    public List<string> CarrierServiceInstructions { get; set; } = new();
    public List<string> CustomsClearanceServiceInstructions { get; set; } = new();
    public List<string> ForwarderServiceInstructions { get; set; } = new();
    public List<string> SpecialServiceInstructions { get; set; } = new();
    public string SequenceId { get; set; } = string.Empty;
    public string ShippingPriorityLevelCode { get; set; } = string.Empty;
    public string HandlingCode { get; set; } = string.Empty;
    public List<string> HandlingInstructions { get; set; } = new();
    public string TariffDescription { get; set; } = string.Empty;
    public string TariffCode { get; set; } = string.Empty;
    public decimal? InsurancePremiumAmount { get; set; }
    public decimal? InsuranceValueAmount { get; set; }
    public decimal? DeclaredCustomsValueAmount { get; set; }
    public decimal? DeclaredForCarriageValueAmount { get; set; }
    public decimal? DeclaredStatisticsValueAmount { get; set; }
    public decimal? FreeOnBoardValueAmount { get; set; }
    public List<string> SpecialInstructions { get; set; } = new();
    public bool? SplitConsignmentIndicator { get; set; }
    public List<DeliveryInstructions> DeliveryInstructions { get; set; } = new();
    public InvoiceParty? ConsigneeParty { get; set; }
    public InvoiceParty? ExporterParty { get; set; }
    public InvoiceParty? ConsignorParty { get; set; }
    public InvoiceParty? ImporterParty { get; set; }
    public InvoiceParty? CarrierParty { get; set; }
    public InvoiceParty? FreightForwarderParty { get; set; }
    public InvoiceParty? NotifyParty { get; set; }
    public InvoiceParty? OriginalDespatchParty { get; set; }
    public InvoiceParty? FinalDeliveryParty { get; set; }
    public InvoiceParty? PerformingCarrierParty { get; set; }
    public InvoiceParty? SubstituteCarrierParty { get; set; }
    public InvoiceParty? LogisticsOperatorParty { get; set; }
    public InvoiceParty? TransportAdvisorParty { get; set; }
    public InvoiceParty? HazardousItemNotificationParty { get; set; }
    public InvoiceParty? InsuranceParty { get; set; }
    public InvoiceParty? MortgageHolderParty { get; set; }
    public InvoiceParty? BillLadingHolderParty { get; set; }
    public Address? OriginalDepartureCountry { get; set; }
    public Address? FinalDestinationCountry { get; set; }
    public List<Address> TransitCountries { get; set; } = new();
    public TransportContract? TransportContract { get; set; }
    public List<TransportEvent> TransportEvents { get; set; } = new();
    public OriginalDespatchTransportation? OriginalDespatchTransportation { get; set; }
    public FinalDeliveryTransportation? FinalDeliveryTransportation { get; set; }
    public List<DeliveryTerms> DeliveryTerms { get; set; } = new();
    public PaymentTerms? PaymentTerms { get; set; }
    public List<FreightAllowanceCharge> FreightAllowanceCharges { get; set; } = new();
}

/// <summary>
/// Delivery instructions
/// </summary>
public class DeliveryInstructions
{
    public string DeliveryNote { get; set; } = string.Empty;
    public string DeliveryToLocationId { get; set; } = string.Empty;
    public string DeliveryToLocationName { get; set; } = string.Empty;
    public string CarrierAssignedDeliveryId { get; set; } = string.Empty;
}

/// <summary>
/// Transport contract
/// </summary>
public class TransportContract
{
    public string Id { get; set; } = string.Empty;
    public string ContractTypeCode { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public List<DocumentReference> ContractDocumentReferences { get; set; } = new();
}

/// <summary>
/// Transport event
/// </summary>
public class TransportEvent
{
    public string IdentificationId { get; set; } = string.Empty;
    public DateTime? OccurrenceDate { get; set; }
    public string OccurrenceTime { get; set; } = string.Empty;
    public string TransportEventTypeCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool? CompletionIndicator { get; set; }
    public ShipmentStage? ReportedShipmentStage { get; set; }
    public List<ContactInformation> CurrentStatus { get; set; } = new();
    public List<ContactInformation> Contacts { get; set; } = new();
    public Address? Location { get; set; }
    public Signature? Signature { get; set; }
    public List<Period> Periods { get; set; } = new();
}

/// <summary>
/// Signature
/// </summary>
public class Signature
{
    public string Id { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime? ValidationDate { get; set; }
    public string ValidationTime { get; set; } = string.Empty;
    public string ValidatorId { get; set; } = string.Empty;
    public string CanonicalizationMethod { get; set; } = string.Empty;
    public string SignatureMethod { get; set; } = string.Empty;
    public InvoiceParty? SignatoryParty { get; set; }
    public DigitalSignatureAttachment? DigitalSignatureAttachment { get; set; }
    public OriginalDocumentReference? OriginalDocumentReference { get; set; }
}

/// <summary>
/// Digital signature attachment
/// </summary>
public class DigitalSignatureAttachment
{
    public string ExternalReference { get; set; } = string.Empty;
    public byte[]? EmbeddedDocumentBinaryObject { get; set; }
}

/// <summary>
/// Original document reference
/// </summary>
public class OriginalDocumentReference
{
    public string Id { get; set; } = string.Empty;
    public bool? CopyIndicator { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public string IssueTime { get; set; } = string.Empty;
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public List<string> XPath { get; set; } = new();
    public string LanguageId { get; set; } = string.Empty;
    public string LocaleCode { get; set; } = string.Empty;
    public string VersionId { get; set; } = string.Empty;
    public string DocumentStatusCode { get; set; } = string.Empty;
    public string DocumentDescription { get; set; } = string.Empty;
    public InvoiceDocument? Attachment { get; set; }
    public Period? ValidityPeriod { get; set; }
    public InvoiceParty? IssuerParty { get; set; }
    public ResultOfVerification? ResultOfVerification { get; set; }
}

/// <summary>
/// Result of verification
/// </summary>
public class ResultOfVerification
{
    public string ValidatorId { get; set; } = string.Empty;
    public string ValidationResultCode { get; set; } = string.Empty;
    public DateTime? ValidationDate { get; set; }
    public string ValidationTime { get; set; } = string.Empty;
    public string ValidateProcess { get; set; } = string.Empty;
    public string ValidateTool { get; set; } = string.Empty;
    public string ValidateToolVersion { get; set; } = string.Empty;
    public InvoiceParty? SignatoryParty { get; set; }
}

/// <summary>
/// Period
/// </summary>
public class Period
{
    public DateTime? StartDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public string EndTime { get; set; } = string.Empty;
    public string DurationMeasure { get; set; } = string.Empty;
    public List<string> DescriptionCodes { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Original despatch transportation
/// </summary>
public class OriginalDespatchTransportation
{
    public string TransportationModeCode { get; set; } = string.Empty;
    public string TransportMeansTypeCode { get; set; } = string.Empty;
    public string JourneyId { get; set; } = string.Empty;
    public string Information { get; set; } = string.Empty;
}

/// <summary>
/// Final delivery transportation
/// </summary>
public class FinalDeliveryTransportation
{
    public string TransportationModeCode { get; set; } = string.Empty;
    public string TransportMeansTypeCode { get; set; } = string.Empty;
    public string JourneyId { get; set; } = string.Empty;
    public string Information { get; set; } = string.Empty;
}

/// <summary>
/// Freight allowance charge
/// </summary>
public class FreightAllowanceCharge
{
    public bool ChargeIndicator { get; set; }
    public string AllowanceChargeReasonCode { get; set; } = string.Empty;
    public string AllowanceChargeReason { get; set; } = string.Empty;
    public decimal? SequenceNumeric { get; set; }
    public decimal Amount { get; set; }
    public decimal? BaseAmount { get; set; }
    public string AccountingCostCode { get; set; } = string.Empty;
    public string AccountingCost { get; set; } = string.Empty;
    public bool? PrepaidIndicator { get; set; }
}

/// <summary>
/// Goods item
/// </summary>
public class GoodsItem
{
    public string Id { get; set; } = string.Empty;
    public string SequenceNumberId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool? HazardousRiskIndicator { get; set; }
    public decimal? DeclaredCustomsValueAmount { get; set; }
    public decimal? DeclaredForCarriageValueAmount { get; set; }
    public decimal? DeclaredStatisticsValueAmount { get; set; }
    public decimal? FreeOnBoardValueAmount { get; set; }
    public decimal? InsuranceValueAmount { get; set; }
    public decimal? ValueAmount { get; set; }
    public decimal? GrossWeightMeasure { get; set; }
    public decimal? NetWeightMeasure { get; set; }
    public decimal? NetNetWeightMeasure { get; set; }
    public decimal? ChargeableWeightMeasure { get; set; }
    public decimal? GrossVolumeMeasure { get; set; }
    public decimal? NetVolumeMeasure { get; set; }
    public decimal? Quantity { get; set; }
    public bool? PreferenceCriterionIndicator { get; set; }
    public string RequiredCustomsId { get; set; } = string.Empty;
    public string CustomsStatusCode { get; set; } = string.Empty;
    public decimal? CustomsTariffQuantity { get; set; }
    public bool? CustomsImportClassifiedIndicator { get; set; }
    public decimal? ChargeableQuantity { get; set; }
    public decimal? ReturnableQuantity { get; set; }
    public decimal? TraceId { get; set; }
    public List<ItemDetails> Items { get; set; } = new();
    public List<GoodsItemContainer> GoodsItemContainers { get; set; } = new();
    public List<FreightAllowanceCharge> FreightAllowanceCharges { get; set; } = new();
    public List<InvoiceLine> InvoiceLines { get; set; } = new();
    public List<Temperature> Temperatures { get; set; } = new();
    public Address? OriginAddress { get; set; }
    public DeliveryInformation? Delivery { get; set; }
    public Pickup? Pickup { get; set; }
    public Despatch? Despatch { get; set; }
    public List<MeasurementDimension> MeasurementDimensions { get; set; } = new();
    public List<ContainedGoodsItem> ContainedGoodsItems { get; set; } = new();
}

/// <summary>
/// Goods item container
/// </summary>
public class GoodsItemContainer
{
    public string Id { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public List<TransportEquipment> TransportEquipment { get; set; } = new();
}

/// <summary>
/// Transport equipment
/// </summary>
public class TransportEquipment
{
    public string Id { get; set; } = string.Empty;
    public List<string> ReferencedConsignmentIds { get; set; } = new();
    public string TransportEquipmentTypeCode { get; set; } = string.Empty;
    public string ProviderTypeCode { get; set; } = string.Empty;
    public string OwnerTypeCode { get; set; } = string.Empty;
    public string SizeTypeCode { get; set; } = string.Empty;
    public string DispositionCode { get; set; } = string.Empty;
    public string FullnessIndicationCode { get; set; } = string.Empty;
    public bool? RefrigerationOnIndicator { get; set; }
    public string Information { get; set; } = string.Empty;
    public bool? ReturnabilityIndicator { get; set; }
    public bool? LegalStatusIndicator { get; set; }
    public bool? AirFlowIndicator { get; set; }
    public bool? HumidityIndicator { get; set; }
    public bool? AnimalFoodApprovedIndicator { get; set; }
    public bool? HumanFoodApprovedIndicator { get; set; }
    public bool? DangerousGoodsApprovedIndicator { get; set; }
    public decimal? RefrigeratedIndicator { get; set; }
    public string Characteristics { get; set; } = string.Empty;
    public List<string> DamageRemarks { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public List<string> SpecialTransportRequirements { get; set; } = new();
    public decimal? GrossWeightMeasure { get; set; }
    public decimal? GrossVolumeMeasure { get; set; }
    public decimal? TareWeightMeasure { get; set; }
    public decimal? TrackingDeviceCode { get; set; }
    public bool? PowerIndicator { get; set; }
    public bool? TraceIndicator { get; set; }
    public List<MeasurementDimension> MeasurementDimensions { get; set; } = new();
    public List<TransportEquipmentSeal> TransportEquipmentSeals { get; set; } = new();
    public decimal? MinimumTemperature { get; set; }
    public decimal? MaximumTemperature { get; set; }
    public InvoiceParty? ProviderParty { get; set; }
    public InvoiceParty? LoadingProofParty { get; set; }
    public InvoiceParty? SupplierParty { get; set; }
    public InvoiceParty? OwnerParty { get; set; }
    public InvoiceParty? OperatingParty { get; set; }
    public Address? LoadingLocation { get; set; }
    public Address? UnloadingLocation { get; set; }
    public Address? StorageLocation { get; set; }
    public List<TransportEvent> TransportEvents { get; set; } = new();
    public AirTransport? AirTransport { get; set; }
    public RoadTransport? RoadTransport { get; set; }
    public RailTransport? RailTransport { get; set; }
    public MaritimeTransport? MaritimeTransport { get; set; }
    public List<TransportHandlingUnit> PackagedTransportHandlingUnits { get; set; } = new();
    public List<TransportHandlingUnit> ServiceAllowanceCharges { get; set; } = new();
    public List<FreightAllowanceCharge> FreightAllowanceCharges { get; set; } = new();
    public List<TransportHandlingUnit> AttachedTransportEquipment { get; set; } = new();
    public Delivery? Delivery { get; set; }
    public Pickup? Pickup { get; set; }
    public Despatch? Despatch { get; set; }
    public List<ShipmentDocumentReference> ShipmentDocumentReferences { get; set; } = new();
    public List<GoodsItem> ContainedInTransportEquipment { get; set; } = new();
    public List<Package> Packages { get; set; } = new();
}

/// <summary>
/// Measurement dimension
/// </summary>
public class MeasurementDimension
{
    public string AttributeId { get; set; } = string.Empty;
    public decimal Measure { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? MinimumMeasure { get; set; }
    public decimal? MaximumMeasure { get; set; }
}

/// <summary>
/// Transport equipment seal
/// </summary>
public class TransportEquipmentSeal
{
    public string Id { get; set; } = string.Empty;
    public string SealIssuerTypeCode { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string SealStatusCode { get; set; } = string.Empty;
    public string SealingPartyType { get; set; } = string.Empty;
}

/// <summary>
/// Air transport
/// </summary>
public class AirTransport
{
    public string AircraftId { get; set; } = string.Empty;
}

/// <summary>
/// Road transport
/// </summary>
public class RoadTransport
{
    public string LicensePlateId { get; set; } = string.Empty;
}

/// <summary>
/// Rail transport
/// </summary>
public class RailTransport
{
    public string TrainId { get; set; } = string.Empty;
    public string RailCarId { get; set; } = string.Empty;
}

/// <summary>
/// Maritime transport
/// </summary>
public class MaritimeTransport
{
    public string VesselId { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string RadioCallSignId { get; set; } = string.Empty;
    public List<string> ShipsRequirements { get; set; } = new();
    public decimal? GrossTonnageMeasure { get; set; }
    public decimal? NetTonnageMeasure { get; set; }
    public Address? RegistryPortLocation { get; set; }
}

/// <summary>
/// Transport handling unit
/// </summary>
public class TransportHandlingUnit
{
    public string Id { get; set; } = string.Empty;
    public string TransportHandlingUnitTypeCode { get; set; } = string.Empty;
    public string HandlingCode { get; set; } = string.Empty;
    public List<string> HandlingInstructions { get; set; } = new();
    public bool? HazardousRiskIndicator { get; set; }
    public decimal? TotalGoodsItemQuantity { get; set; }
    public decimal? TotalPackageQuantity { get; set; }
    public List<string> DamageRemarks { get; set; } = new();
    public List<string> ShippingMarks { get; set; } = new();
    public bool? TraceIndicator { get; set; }
    public List<TransportHandlingUnit> HandlingUnitDespatchLines { get; set; } = new();
    public List<Package> ActualPackages { get; set; } = new();
    public List<TransportHandlingUnit> ReceivedHandlingUnitReceiptLines { get; set; } = new();
    public List<TransportEquipment> TransportEquipment { get; set; } = new();
    public List<TransportMeans> TransportMeans { get; set; } = new();
    public List<HazardousItem> HazardousItems { get; set; } = new();
    public List<MeasurementDimension> MeasurementDimensions { get; set; } = new();
    public decimal? MinimumTemperature { get; set; }
    public decimal? MaximumTemperature { get; set; }
    public List<GoodsItem> TransportHandlingUnitDespatchLines { get; set; } = new();
    public List<ShipmentDocumentReference> ShipmentDocumentReferences { get; set; } = new();
    public Status? Status { get; set; }
    public List<CustomsDeclaration> CustomsDeclarations { get; set; } = new();
    public List<TransportEvent> TransportEvents { get; set; } = new();
    public Pickup? Pickup { get; set; }
    public Delivery? Delivery { get; set; }
    public Despatch? Despatch { get; set; }
    public List<TransportHandlingUnit> ChildTransportHandlingUnits { get; set; } = new();
}

/// <summary>
/// Transport means
/// </summary>
public class TransportMeans
{
    public string JourneyId { get; set; } = string.Empty;
    public string RegistrationNationalityId { get; set; } = string.Empty;
    public List<string> RegistrationNationalities { get; set; } = new();
    public string DirectionCode { get; set; } = string.Empty;
    public string TransportMeansTypeCode { get; set; } = string.Empty;
    public string TradeServiceCode { get; set; } = string.Empty;
    public AirTransport? AirTransport { get; set; }
    public RoadTransport? RoadTransport { get; set; }
    public RailTransport? RailTransport { get; set; }
    public MaritimeTransport? MaritimeTransport { get; set; }
    public InvoiceParty? OwnerParty { get; set; }
    public List<MeasurementDimension> MeasurementDimensions { get; set; } = new();
}

/// <summary>
/// Hazardous item
/// </summary>
public class HazardousItem
{
    public string Id { get; set; } = string.Empty;
    public string PlacardNotation { get; set; } = string.Empty;
    public string PlacardEndorsement { get; set; } = string.Empty;
    public string AdditionalInformation { get; set; } = string.Empty;
    public string UndgCode { get; set; } = string.Empty;
    public bool? EmergencyProceduresIndicator { get; set; }
    public bool? MedicalFirstAidGuideIndicator { get; set; }
    public string TechnicalName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string HazardousCategoryCode { get; set; } = string.Empty;
    public string UpperOrangeHazardPlacardId { get; set; } = string.Empty;
    public string LowerOrangeHazardPlacardId { get; set; } = string.Empty;
    public string MarkingId { get; set; } = string.Empty;
    public string HazardClassId { get; set; } = string.Empty;
    public decimal? NetWeightMeasure { get; set; }
    public decimal? NetVolumeMeasure { get; set; }
    public decimal? Quantity { get; set; }
    public InvoiceParty? ContactParty { get; set; }
    public List<SecondaryHazard> SecondaryHazards { get; set; } = new();
    public List<HazardousItemTemperature> HazardousItemTemperatures { get; set; } = new();
    public List<HazardousItemFlashpoint> HazardousItemFlashpoints { get; set; } = new();
    public List<string> AdditionalTemperatures { get; set; } = new();
}

/// <summary>
/// Secondary hazard
/// </summary>
public class SecondaryHazard
{
    public string Id { get; set; } = string.Empty;
    public string PlacardNotation { get; set; } = string.Empty;
    public string PlacardEndorsement { get; set; } = string.Empty;
    public bool? EmergencyProceduresIndicator { get; set; }
    public string Extension { get; set; } = string.Empty;
}

/// <summary>
/// Hazardous item temperature
/// </summary>
public class HazardousItemTemperature
{
    public decimal AttributeId { get; set; }
    public decimal Measure { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Hazardous item flashpoint
/// </summary>
public class HazardousItemFlashpoint
{
    public decimal AttributeId { get; set; }
    public decimal Measure { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Package
/// </summary>
public class Package
{
    public string Id { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public bool? ReturnableMaterialIndicator { get; set; }
    public string PackageLevelCode { get; set; } = string.Empty;
    public string PackagingTypeCode { get; set; } = string.Empty;
    public List<string> PackingMaterials { get; set; } = new();
    public List<Package> ContainedPackages { get; set; } = new();
    public InvoiceParty? ContainingTransportEquipment { get; set; }
    public List<GoodsItem> GoodsItems { get; set; } = new();
    public List<MeasurementDimension> MeasurementDimensions { get; set; } = new();
    public List<DeliveryUnit> DeliveryUnits { get; set; } = new();
    public Delivery? Delivery { get; set; }
    public Pickup? Pickup { get; set; }
    public Despatch? Despatch { get; set; }
}

/// <summary>
/// Delivery unit
/// </summary>
public class DeliveryUnit
{
    public string BatchId { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? BestBeforeDate { get; set; }
}

/// <summary>
/// Shipment document reference
/// </summary>
public class ShipmentDocumentReference
{
    public string Id { get; set; } = string.Empty;
    public bool? CopyIndicator { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public List<string> XPath { get; set; } = new();
    public InvoiceDocument? Attachment { get; set; }
}

/// <summary>
/// Status
/// </summary>
public class Status
{
    public string ConditionCode { get; set; } = string.Empty;
    public DateTime? ReferenceDate { get; set; }
    public string ReferenceTime { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusReasonCode { get; set; } = string.Empty;
    public string StatusReason { get; set; } = string.Empty;
    public string SequenceId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool? IndicationIndicator { get; set; }
    public decimal? Percent { get; set; }
    public decimal? ReliabilityPercent { get; set; }
    public List<Condition> Conditions { get; set; } = new();
}

/// <summary>
/// Condition
/// </summary>
public class Condition
{
    public string AttributeId { get; set; } = string.Empty;
    public decimal? Measure { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? MinimumMeasure { get; set; }
    public decimal? MaximumMeasure { get; set; }
}

/// <summary>
/// Customs declaration
/// </summary>
public class CustomsDeclaration
{
    public string Id { get; set; } = string.Empty;
    public DateTime? ValidityDate { get; set; }
    public string ValidityTime { get; set; } = string.Empty;
    public string RequiredCustomsId { get; set; } = string.Empty;
    public string CustomsStatusCode { get; set; } = string.Empty;
    public decimal? CustomsTariffQuantity { get; set; }
    public bool? CustomsImportClassifiedIndicator { get; set; }
    public InvoiceParty? IssuerParty { get; set; }
}

/// <summary>
/// Pickup
/// </summary>
public class Pickup
{
    public string Id { get; set; } = string.Empty;
    public DateTime? ActualPickupDate { get; set; }
    public string ActualPickupTime { get; set; } = string.Empty;
    public DateTime? EarliestPickupDate { get; set; }
    public string EarliestPickupTime { get; set; } = string.Empty;
    public DateTime? LatestPickupDate { get; set; }
    public string LatestPickupTime { get; set; } = string.Empty;
    public Address? PickupLocation { get; set; }
    public InvoiceParty? PickupParty { get; set; }
}

/// <summary>
/// Despatch
/// </summary>
public class Despatch
{
    public string Id { get; set; } = string.Empty;
    public DateTime? RequestedDespatchDate { get; set; }
    public string RequestedDespatchTime { get; set; } = string.Empty;
    public DateTime? EstimatedDespatchDate { get; set; }
    public string EstimatedDespatchTime { get; set; } = string.Empty;
    public DateTime? ActualDespatchDate { get; set; }
    public string ActualDespatchTime { get; set; } = string.Empty;
    public DateTime? GuaranteedDespatchDate { get; set; }
    public string GuaranteedDespatchTime { get; set; } = string.Empty;
    public string ReleaseId { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public Address? DespatchAddress { get; set; }
    public InvoiceParty? DespatchParty { get; set; }
    public InvoiceParty? CarrierParty { get; set; }
    public ContactInformation? NotifyParty { get; set; }
    public Address? DespatchLocation { get; set; }
}

/// <summary>
/// Delivery
/// </summary>
public class Delivery
{
    public string Id { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? MinimumQuantity { get; set; }
    public decimal? MaximumQuantity { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string ActualDeliveryTime { get; set; } = string.Empty;
    public DateTime? LatestDeliveryDate { get; set; }
    public string LatestDeliveryTime { get; set; } = string.Empty;
    public string ReleaseId { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public Address? DeliveryLocation { get; set; }
    public Period? RequestedDeliveryPeriod { get; set; }
    public Period? PromisedDeliveryPeriod { get; set; }
    public Period? EstimatedDeliveryPeriod { get; set; }
    public InvoiceParty? CarrierParty { get; set; }
    public InvoiceParty? DeliveryParty { get; set; }
    public InvoiceParty? NotifyParty { get; set; }
    public Despatch? Despatch { get; set; }
    public List<DeliveryTerms> DeliveryTerms { get; set; } = new();
    public decimal? MinimumDeliveryUnit { get; set; }
    public decimal? MaximumDeliveryUnit { get; set; }
    public Shipment? Shipment { get; set; }
}

/// <summary>
/// Contained goods item
/// </summary>
public class ContainedGoodsItem
{
    public string Id { get; set; } = string.Empty;
    public string SequenceNumberId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool? HazardousRiskIndicator { get; set; }
    public decimal? DeclaredCustomsValueAmount { get; set; }
    public decimal? DeclaredForCarriageValueAmount { get; set; }
    public decimal? DeclaredStatisticsValueAmount { get; set; }
    public decimal? FreeOnBoardValueAmount { get; set; }
    public decimal? InsuranceValueAmount { get; set; }
    public decimal? ValueAmount { get; set; }
    public decimal? GrossWeightMeasure { get; set; }
    public decimal? NetWeightMeasure { get; set; }
    public decimal? NetNetWeightMeasure { get; set; }
    public decimal? ChargeableWeightMeasure { get; set; }
    public decimal? GrossVolumeMeasure { get; set; }
    public decimal? NetVolumeMeasure { get; set; }
    public decimal? Quantity { get; set; }
    public bool? PreferenceCriterionIndicator { get; set; }
    public string RequiredCustomsId { get; set; } = string.Empty;
    public string CustomsStatusCode { get; set; } = string.Empty;
    public decimal? CustomsTariffQuantity { get; set; }
    public bool? CustomsImportClassifiedIndicator { get; set; }
    public decimal? ChargeableQuantity { get; set; }
    public decimal? ReturnableQuantity { get; set; }
    public decimal? TraceId { get; set; }
    public List<ItemDetails> Items { get; set; } = new();
    public List<GoodsItemContainer> GoodsItemContainers { get; set; } = new();
    public List<FreightAllowanceCharge> FreightAllowanceCharges { get; set; } = new();
    public List<InvoiceLine> InvoiceLines { get; set; } = new();
    public List<Temperature> Temperatures { get; set; } = new();
    public Address? OriginAddress { get; set; }
    public List<MeasurementDimension> MeasurementDimensions { get; set; } = new();
}

/// <summary>
/// Temperature
/// </summary>
public class Temperature
{
    public decimal AttributeId { get; set; }
    public decimal Measure { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Shipment stage
/// </summary>
public class ShipmentStage
{
    public string Id { get; set; } = string.Empty;
    public string TransportModeCode { get; set; } = string.Empty;
    public string TransportMeansTypeCode { get; set; } = string.Empty;
    public string TransitDirectionCode { get; set; } = string.Empty;
    public bool? PreCarriageIndicator { get; set; }
    public bool? OnCarriageIndicator { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public string EstimatedDeliveryTime { get; set; } = string.Empty;
    public DateTime? RequiredDeliveryDate { get; set; }
    public string RequiredDeliveryTime { get; set; } = string.Empty;
    public string LoadingSequenceId { get; set; } = string.Empty;
    public string SuccessiveSequenceId { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public List<string> DemurrageInstructions { get; set; } = new();
    public decimal? CrewQuantity { get; set; }
    public decimal? PassengerQuantity { get; set; }
    public Period? TransitPeriod { get; set; }
    public List<InvoiceParty> CarrierParties { get; set; } = new();
    public TransportMeans? TransportMeans { get; set; }
    public Address? LoadingPortLocation { get; set; }
    public Address? UnloadingPortLocation { get; set; }
    public Address? TransshipPortLocation { get; set; }
    public Address? LoadingTransportEvent { get; set; }
    public Address? ExaminationTransportEvent { get; set; }
    public Address? AvailabilityTransportEvent { get; set; }
    public Address? ExportationTransportEvent { get; set; }
    public Address? DischargeTransportEvent { get; set; }
    public Address? WarehousingTransportEvent { get; set; }
    public Address? TakeoverTransportEvent { get; set; }
    public Address? OptionalTakeoverTransportEvent { get; set; }
    public Address? DropoffTransportEvent { get; set; }
    public Address? ActualPickupTransportEvent { get; set; }
    public Address? DeliveryTransportEvent { get; set; }
    public Address? ReceiptTransportEvent { get; set; }
    public Address? StorageTransportEvent { get; set; }
    public Address? AcceptanceTransportEvent { get; set; }
    public Address? TerminalOperatorParty { get; set; }
    public Address? CustomsAgentParty { get; set; }
    public Address? EstimatedTransitPeriod { get; set; }
    public List<FreightAllowanceCharge> FreightAllowanceCharges { get; set; } = new();
    public Address? FreightChargeLocation { get; set; }
    public List<DetentionTransportEvent> DetentionTransportEvents { get; set; } = new();
    public Address? RequestedArrivalTransportEvent { get; set; }
    public Address? RequestedDepartureTransportEvent { get; set; }
    public Address? ElapsedTransportEvent { get; set; }
    public List<TransportEvent> EstimatedArrivalTransportEvents { get; set; } = new();
    public List<TransportEvent> EstimatedDepartureTransportEvents { get; set; } = new();
    public List<TransportEvent> ActualArrivalTransportEvents { get; set; } = new();
    public List<TransportEvent> ActualDepartureTransportEvents { get; set; } = new();
    public List<TransportEvent> TransportEvents { get; set; } = new();
    public Address? EstimatedArrivalTransportEvent { get; set; }
    public List<InvoiceParty> DriverPersons { get; set; } = new();
}

/// <summary>
/// Detention transport event
/// </summary>
public class DetentionTransportEvent
{
    public string DetentionTransportEventTypeCode { get; set; } = string.Empty;
    public DateTime? DetentionTransportEventDate { get; set; }
    public string DetentionTransportEventTime { get; set; } = string.Empty;
}

/// <summary>
/// Country-specific validation models
/// </summary>
public class CountryValidationRule
{
    public string Country { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Request models
/// </summary>
public class CreateElectronicInvoiceRequest
{
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    [Required]
    public InvoiceType Type { get; set; }
    
    [Required]
    public InvoiceParty Issuer { get; set; } = new();
    
    [Required]
    public InvoiceParty Customer { get; set; } = new();
    
    [Required]
    public List<InvoiceLine> Lines { get; set; } = new();
    
    public string Currency { get; set; } = "USD";
    public DateTime? DueDate { get; set; }
    public List<PaymentInformation> PaymentInformation { get; set; } = new();
    public DeliveryInformation? DeliveryInformation { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

/// <summary>
/// Electronic invoicing result wrapper
/// </summary>
public class EInvoicingResult
{
    public bool IsSuccess { get; set; }
    public ElectronicInvoice? Invoice { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public List<ValidationWarning> ValidationWarnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static EInvoicingResult Success(ElectronicInvoice invoice) =>
        new() { IsSuccess = true, Invoice = invoice };

    public static EInvoicingResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static EInvoicingResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}

/// <summary>
/// Validation error for e-invoicing
/// </summary>
public class ValidationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }
    public object? AttemptedValue { get; set; }
    public string Severity { get; set; } = "Error";
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Validation warning for e-invoicing
/// </summary>
public class ValidationWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}