using BizCore.EInvoicing.Models;

namespace BizCore.EInvoicing.Interfaces;

/// <summary>
/// Digital signature service interface for electronic invoices
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>
    /// Sign invoice digitally
    /// </summary>
    Task<DigitalSignature> SignInvoiceAsync(ElectronicInvoice invoice, CertificateInfo certificate, string signingKey);

    /// <summary>
    /// Verify digital signature
    /// </summary>
    Task<SignatureValidation> VerifySignatureAsync(DigitalSignature signature, ElectronicInvoice invoice);

    /// <summary>
    /// Generate timestamp token
    /// </summary>
    Task<string> GenerateTimestampTokenAsync(DigitalSignature signature);

    /// <summary>
    /// Validate certificate
    /// </summary>
    Task<bool> ValidateCertificateAsync(CertificateInfo certificate, string countryCode);

    /// <summary>
    /// Get certificate chain
    /// </summary>
    Task<IEnumerable<CertificateInfo>> GetCertificateChainAsync(CertificateInfo certificate);

    /// <summary>
    /// Check certificate revocation status
    /// </summary>
    Task<bool> CheckCertificateRevocationAsync(CertificateInfo certificate);

    /// <summary>
    /// Get signing algorithms for country
    /// </summary>
    Task<IEnumerable<string>> GetSupportedAlgorithmsAsync(string countryCode);

    /// <summary>
    /// Create certificate request
    /// </summary>
    Task<Dictionary<string, object>> CreateCertificateRequestAsync(InvoiceParty party, string countryCode);

    /// <summary>
    /// Install certificate
    /// </summary>
    Task<bool> InstallCertificateAsync(byte[] certificateData, string password, string tenantId);

    /// <summary>
    /// List available certificates
    /// </summary>
    Task<IEnumerable<CertificateInfo>> ListCertificatesAsync(string tenantId);

    /// <summary>
    /// Export certificate
    /// </summary>
    Task<byte[]> ExportCertificateAsync(string certificateId, string format = "PFX");

    /// <summary>
    /// Renew certificate
    /// </summary>
    Task<CertificateInfo> RenewCertificateAsync(string certificateId);
}