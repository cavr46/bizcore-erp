namespace BizCore.Backup.Interfaces;

/// <summary>
/// Backup encryption service interface
/// </summary>
public interface IBackupEncryptionService
{
    /// <summary>
    /// Encrypt data stream
    /// </summary>
    Task<Stream> EncryptAsync(Stream data, EncryptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt data stream
    /// </summary>
    Task<Stream> DecryptAsync(Stream encryptedData, DecryptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate encryption key
    /// </summary>
    Task<EncryptionKey> GenerateKeyAsync(EncryptionAlgorithm algorithm);

    /// <summary>
    /// Store encryption key securely
    /// </summary>
    Task<string> StoreKeyAsync(EncryptionKey key, string keyId);

    /// <summary>
    /// Retrieve encryption key
    /// </summary>
    Task<EncryptionKey?> RetrieveKeyAsync(string keyId);

    /// <summary>
    /// Rotate encryption key
    /// </summary>
    Task<EncryptionKey> RotateKeyAsync(string oldKeyId);

    /// <summary>
    /// Delete encryption key
    /// </summary>
    Task<bool> DeleteKeyAsync(string keyId);

    /// <summary>
    /// Encrypt file
    /// </summary>
    Task<EncryptionResult> EncryptFileAsync(string inputPath, string outputPath, EncryptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt file
    /// </summary>
    Task<DecryptionResult> DecryptFileAsync(string inputPath, string outputPath, DecryptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate hash
    /// </summary>
    Task<string> CalculateHashAsync(Stream data, string algorithm = "SHA256", CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify integrity
    /// </summary>
    Task<bool> VerifyIntegrityAsync(Stream data, string expectedHash, string algorithm = "SHA256", CancellationToken cancellationToken = default);
}

/// <summary>
/// Encryption options
/// </summary>
public class EncryptionOptions
{
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;
    public string? KeyId { get; set; }
    public byte[]? Key { get; set; }
    public byte[]? IV { get; set; }
    public bool GenerateIV { get; set; } = true;
    public bool IncludeHeader { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Decryption options
/// </summary>
public class DecryptionOptions
{
    public string? KeyId { get; set; }
    public byte[]? Key { get; set; }
    public byte[]? IV { get; set; }
    public bool VerifyIntegrity { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Encryption key
/// </summary>
public class EncryptionKey
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public EncryptionAlgorithm Algorithm { get; set; }
    public byte[] KeyMaterial { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ParentKeyId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Encryption result
/// </summary>
public class EncryptionResult
{
    public bool IsSuccess { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public long EncryptedSize { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Decryption result
/// </summary>
public class DecryptionResult
{
    public bool IsSuccess { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public long DecryptedSize { get; set; }
    public string Hash { get; set; } = string.Empty;
    public bool IntegrityVerified { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}