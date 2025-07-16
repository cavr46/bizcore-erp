using BizCore.EInvoicing.Models;

namespace BizCore.EInvoicing.Interfaces;

/// <summary>
/// Invoice format conversion service interface
/// </summary>
public interface IInvoiceFormatService
{
    /// <summary>
    /// Convert invoice to UBL format
    /// </summary>
    Task<string> ConvertToUBLAsync(ElectronicInvoice invoice, string version = "2.1");

    /// <summary>
    /// Convert invoice to UN/CEFACT CII format
    /// </summary>
    Task<string> ConvertToCIIAsync(ElectronicInvoice invoice, string version = "D16B");

    /// <summary>
    /// Convert invoice to EDIFACT format
    /// </summary>
    Task<string> ConvertToEDIFACTAsync(ElectronicInvoice invoice, string version = "D96A");

    /// <summary>
    /// Convert invoice to country-specific format
    /// </summary>
    Task<string> ConvertToCountryFormatAsync(ElectronicInvoice invoice, string countryCode, string format);

    /// <summary>
    /// Convert invoice to PDF
    /// </summary>
    Task<byte[]> ConvertToPdfAsync(ElectronicInvoice invoice, string template = "default", string language = "en");

    /// <summary>
    /// Convert invoice from external format
    /// </summary>
    Task<ElectronicInvoice> ConvertFromExternalAsync(string data, string format, string countryCode);

    /// <summary>
    /// Validate format
    /// </summary>
    Task<EInvoicingResult> ValidateFormatAsync(string data, string format, string version);

    /// <summary>
    /// Get supported formats for country
    /// </summary>
    Task<IEnumerable<string>> GetSupportedFormatsAsync(string countryCode);

    /// <summary>
    /// Get format schema
    /// </summary>
    Task<string> GetFormatSchemaAsync(string format, string version);

    /// <summary>
    /// Transform between formats
    /// </summary>
    Task<string> TransformFormatAsync(string sourceData, string sourceFormat, string targetFormat, string countryCode);

    /// <summary>
    /// Generate QR code for invoice
    /// </summary>
    Task<byte[]> GenerateQRCodeAsync(ElectronicInvoice invoice, string format = "standard");

    /// <summary>
    /// Extract data from QR code
    /// </summary>
    Task<Dictionary<string, object>> ExtractFromQRCodeAsync(byte[] qrCodeData);
}