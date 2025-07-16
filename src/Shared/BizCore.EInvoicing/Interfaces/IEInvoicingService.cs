using BizCore.EInvoicing.Models;

namespace BizCore.EInvoicing.Interfaces;

/// <summary>
/// Core electronic invoicing service interface with multi-country support
/// </summary>
public interface IEInvoicingService
{
    /// <summary>
    /// Create electronic invoice
    /// </summary>
    Task<EInvoicingResult> CreateInvoiceAsync(CreateElectronicInvoiceRequest request);

    /// <summary>
    /// Update electronic invoice
    /// </summary>
    Task<EInvoicingResult> UpdateInvoiceAsync(string invoiceId, ElectronicInvoice invoice);

    /// <summary>
    /// Get electronic invoice by ID
    /// </summary>
    Task<ElectronicInvoice?> GetInvoiceAsync(string invoiceId);

    /// <summary>
    /// Get invoices for tenant
    /// </summary>
    Task<IEnumerable<ElectronicInvoice>> GetInvoicesAsync(string tenantId, InvoiceStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100);

    /// <summary>
    /// Delete electronic invoice
    /// </summary>
    Task<bool> DeleteInvoiceAsync(string invoiceId);

    /// <summary>
    /// Submit invoice to tax authority
    /// </summary>
    Task<EInvoicingResult> SubmitInvoiceAsync(string invoiceId);

    /// <summary>
    /// Cancel electronic invoice
    /// </summary>
    Task<EInvoicingResult> CancelInvoiceAsync(string invoiceId, string reason);

    /// <summary>
    /// Sign invoice digitally
    /// </summary>
    Task<EInvoicingResult> SignInvoiceAsync(string invoiceId, DigitalSignature signature);

    /// <summary>
    /// Validate invoice against country rules
    /// </summary>
    Task<EInvoicingResult> ValidateInvoiceAsync(string invoiceId, string countryCode);

    /// <summary>
    /// Generate invoice number
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync(string tenantId, InvoiceType type, string series = "");

    /// <summary>
    /// Get invoice status from tax authority
    /// </summary>
    Task<Dictionary<string, object>> GetInvoiceStatusAsync(string invoiceId);

    /// <summary>
    /// Download invoice PDF
    /// </summary>
    Task<byte[]> GenerateInvoicePdfAsync(string invoiceId, string template = "default");

    /// <summary>
    /// Download invoice XML
    /// </summary>
    Task<string> GenerateInvoiceXmlAsync(string invoiceId, string format = "UBL");
}