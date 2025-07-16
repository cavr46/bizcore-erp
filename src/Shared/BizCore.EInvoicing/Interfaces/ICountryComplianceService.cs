using BizCore.EInvoicing.Models;

namespace BizCore.EInvoicing.Interfaces;

/// <summary>
/// Country-specific compliance service interface
/// </summary>
public interface ICountryComplianceService
{
    /// <summary>
    /// Get supported countries
    /// </summary>
    Task<IEnumerable<string>> GetSupportedCountriesAsync();

    /// <summary>
    /// Get country-specific validation rules
    /// </summary>
    Task<IEnumerable<CountryValidationRule>> GetValidationRulesAsync(string countryCode);

    /// <summary>
    /// Validate invoice for specific country
    /// </summary>
    Task<EInvoicingResult> ValidateForCountryAsync(ElectronicInvoice invoice, string countryCode);

    /// <summary>
    /// Get country-specific required fields
    /// </summary>
    Task<Dictionary<string, object>> GetRequiredFieldsAsync(string countryCode, InvoiceType invoiceType);

    /// <summary>
    /// Get tax rates for country
    /// </summary>
    Task<IEnumerable<TaxCategory>> GetTaxRatesAsync(string countryCode);

    /// <summary>
    /// Validate tax information
    /// </summary>
    Task<bool> ValidateTaxIdAsync(string taxId, string countryCode, string taxType = "VAT");

    /// <summary>
    /// Get numbering rules for country
    /// </summary>
    Task<Dictionary<string, object>> GetNumberingRulesAsync(string countryCode);

    /// <summary>
    /// Apply country-specific transformations
    /// </summary>
    Task<ElectronicInvoice> ApplyCountryTransformationsAsync(ElectronicInvoice invoice, string countryCode);

    /// <summary>
    /// Get digital signature requirements
    /// </summary>
    Task<Dictionary<string, object>> GetSignatureRequirementsAsync(string countryCode);

    /// <summary>
    /// Check compliance status
    /// </summary>
    Task<ComplianceStatus> CheckComplianceAsync(ElectronicInvoice invoice, string countryCode);
}