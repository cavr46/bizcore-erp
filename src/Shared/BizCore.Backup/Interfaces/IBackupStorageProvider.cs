namespace BizCore.Backup.Interfaces;

/// <summary>
/// Backup storage provider interface
/// </summary>
public interface IBackupStorageProvider
{
    /// <summary>
    /// Provider name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Provider type
    /// </summary>
    BackupDestinationType Type { get; }

    /// <summary>
    /// Initialize provider with configuration
    /// </summary>
    Task InitializeAsync(Dictionary<string, string> configuration);

    /// <summary>
    /// Upload backup file
    /// </summary>
    Task<BackupUploadResult> UploadAsync(Stream data, string remotePath, BackupUploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download backup file
    /// </summary>
    Task<Stream> DownloadAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete backup file
    /// </summary>
    Task<bool> DeleteAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// List backup files
    /// </summary>
    Task<IEnumerable<BackupFileInfo>> ListAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file exists
    /// </summary>
    Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<BackupFileMetadata> GetMetadataAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connectivity
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get storage statistics
    /// </summary>
    Task<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create multipart upload
    /// </summary>
    Task<string> CreateMultipartUploadAsync(string remotePath, BackupUploadOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload part
    /// </summary>
    Task<string> UploadPartAsync(string uploadId, int partNumber, Stream data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete multipart upload
    /// </summary>
    Task<BackupUploadResult> CompleteMultipartUploadAsync(string uploadId, IEnumerable<string> partETags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Abort multipart upload
    /// </summary>
    Task AbortMultipartUploadAsync(string uploadId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Backup upload options
/// </summary>
public class BackupUploadOptions
{
    public bool EnableEncryption { get; set; } = true;
    public string? EncryptionKey { get; set; }
    public bool EnableCompression { get; set; } = true;
    public string? StorageClass { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public int? RetentionDays { get; set; }
    public bool EnableServerSideEncryption { get; set; } = true;
    public long? PartSize { get; set; }
    public int MaxConcurrentParts { get; set; } = 4;
}

/// <summary>
/// Backup upload result
/// </summary>
public class BackupUploadResult
{
    public bool IsSuccess { get; set; }
    public string RemotePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ETag { get; set; } = string.Empty;
    public string VersionId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public double TransferRateMBps { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Backup file info
/// </summary>
public class BackupFileInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string ETag { get; set; } = string.Empty;
    public string StorageClass { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Backup file metadata
/// </summary>
public class BackupFileMetadata
{
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string ETag { get; set; } = string.Empty;
    public string VersionId { get; set; } = string.Empty;
    public string StorageClass { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public string? EncryptionAlgorithm { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Storage statistics
/// </summary>
public class StorageStatistics
{
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
    public long AvailableSpace { get; set; }
    public int FileCount { get; set; }
    public int DirectoryCount { get; set; }
    public double UsagePercentage { get; set; }
    public Dictionary<string, long> StorageByClass { get; set; } = new();
    public Dictionary<string, object> CustomStatistics { get; set; } = new();
}