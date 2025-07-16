using BizCore.EInvoicing.Models;

namespace BizCore.EInvoicing.Interfaces;

/// <summary>
/// Tax authority integration service interface
/// </summary>
public interface ITaxAuthorityService
{
    /// <summary>
    /// Submit invoice to tax authority
    /// </summary>
    Task<Dictionary<string, object>> SubmitToTaxAuthorityAsync(ElectronicInvoice invoice, string countryCode);

    /// <summary>
    /// Get submission status from tax authority
    /// </summary>
    Task<Dictionary<string, object>> GetSubmissionStatusAsync(string submissionId, string countryCode);

    /// <summary>
    /// Cancel invoice with tax authority
    /// </summary>
    Task<Dictionary<string, object>> CancelWithTaxAuthorityAsync(string invoiceId, string reason, string countryCode);

    /// <summary>
    /// Query invoice status from tax authority
    /// </summary>
    Task<Dictionary<string, object>> QueryInvoiceStatusAsync(string invoiceNumber, string countryCode);

    /// <summary>
    /// Download tax authority response
    /// </summary>
    Task<byte[]> DownloadTaxAuthorityResponseAsync(string submissionId, string countryCode);

    /// <summary>
    /// Validate credentials with tax authority
    /// </summary>
    Task<bool> ValidateCredentialsAsync(string tenantId, string countryCode);

    /// <summary>
    /// Get tax authority requirements
    /// </summary>
    Task<Dictionary<string, object>> GetTaxAuthorityRequirementsAsync(string countryCode);

    /// <summary>
    /// Register taxpayer with tax authority
    /// </summary>
    Task<Dictionary<string, object>> RegisterTaxpayerAsync(InvoiceParty taxpayer, string countryCode);

    /// <summary>
    /// Update taxpayer information
    /// </summary>
    Task<Dictionary<string, object>> UpdateTaxpayerAsync(InvoiceParty taxpayer, string countryCode);

    /// <summary>
    /// Get taxpayer status
    /// </summary>
    Task<Dictionary<string, object>> GetTaxpayerStatusAsync(string taxId, string countryCode);
}