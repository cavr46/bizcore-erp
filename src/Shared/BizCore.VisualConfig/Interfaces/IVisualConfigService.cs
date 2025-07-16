using BizCore.VisualConfig.Models;

namespace BizCore.VisualConfig.Interfaces;

/// <summary>
/// Visual configuration service interface
/// </summary>
public interface IVisualConfigService
{
    /// <summary>
    /// Create a new visual configuration project
    /// </summary>
    Task<VisualConfigResult> CreateProjectAsync(CreateVisualConfigRequest request);

    /// <summary>
    /// Update visual configuration project
    /// </summary>
    Task<VisualConfigResult> UpdateProjectAsync(string projectId, VisualConfigProject project);

    /// <summary>
    /// Get visual configuration project by ID
    /// </summary>
    Task<VisualConfigProject?> GetProjectAsync(string projectId);

    /// <summary>
    /// Query visual configuration projects
    /// </summary>
    Task<IEnumerable<VisualConfigProject>> QueryProjectsAsync(VisualConfigQuery query);

    /// <summary>
    /// Delete visual configuration project
    /// </summary>
    Task<bool> DeleteProjectAsync(string projectId);

    /// <summary>
    /// Validate project configuration
    /// </summary>
    Task<List<ValidationError>> ValidateProjectAsync(string projectId);

    /// <summary>
    /// Deploy project to runtime
    /// </summary>
    Task<VisualConfigResult> DeployProjectAsync(string projectId, DeploymentOptions options);

    /// <summary>
    /// Test project configuration
    /// </summary>
    Task<TestResult> TestProjectAsync(string projectId, TestOptions options);

    /// <summary>
    /// Export project to various formats
    /// </summary>
    Task<ExportResult> ExportProjectAsync(string projectId, ExportOptions options);

    /// <summary>
    /// Import project from external source
    /// </summary>
    Task<VisualConfigResult> ImportProjectAsync(ImportOptions options);

    /// <summary>
    /// Clone project
    /// </summary>
    Task<VisualConfigResult> CloneProjectAsync(string sourceProjectId, CloneOptions options);

    /// <summary>
    /// Get project version history
    /// </summary>
    Task<IEnumerable<ProjectVersion>> GetVersionHistoryAsync(string projectId);

    /// <summary>
    /// Create project snapshot
    /// </summary>
    Task<ProjectSnapshot> CreateSnapshotAsync(string projectId, string description);

    /// <summary>
    /// Restore project from snapshot
    /// </summary>
    Task<VisualConfigResult> RestoreSnapshotAsync(string projectId, string snapshotId);
}

/// <summary>
/// Deployment options
/// </summary>
public class DeploymentOptions
{
    public string Environment { get; set; } = "production";
    public string Version { get; set; } = string.Empty;
    public bool RunTests { get; set; } = true;
    public bool CreateBackup { get; set; } = true;
    public bool NotifyUsers { get; set; } = false;
    public Dictionary<string, object> Variables { get; set; } = new();
    public DeploymentStrategy Strategy { get; set; } = DeploymentStrategy.Replace;
}

/// <summary>
/// Deployment strategies
/// </summary>
public enum DeploymentStrategy
{
    Replace,
    BlueGreen,
    Canary,
    RollingUpdate
}

/// <summary>
/// Test options
/// </summary>
public class TestOptions
{
    public TestType Type { get; set; } = TestType.Validation;
    public string Environment { get; set; } = "test";
    public Dictionary<string, object> TestData { get; set; } = new();
    public bool IncludeIntegration { get; set; } = false;
    public bool IncludePerformance { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Test types
/// </summary>
public enum TestType
{
    Validation,
    Unit,
    Integration,
    Performance,
    Security,
    Accessibility,
    All
}

/// <summary>
/// Test result
/// </summary>
public class TestResult
{
    public bool IsSuccess { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<TestCase> TestCases { get; set; } = new();
    public TestMetrics Metrics { get; set; } = new();
    public string ReportUrl { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Test case
/// </summary>
public class TestCase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TestStatus Status { get; set; } = TestStatus.Pending;
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Test status
/// </summary>
public enum TestStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Skipped,
    Error
}

/// <summary>
/// Test metrics
/// </summary>
public class TestMetrics
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
}

/// <summary>
/// Export options
/// </summary>
public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.JSON;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeDocumentation { get; set; } = true;
    public bool IncludeVersionHistory { get; set; } = false;
    public string? TemplateId { get; set; }
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Export formats
/// </summary>
public enum ExportFormat
{
    JSON,
    XML,
    YAML,
    Code,
    Documentation,
    Template,
    Package,
    Custom
}

/// <summary>
/// Export result
/// </summary>
public class ExportResult
{
    public bool IsSuccess { get; set; }
    public ExportFormat Format { get; set; }
    public string Content { get; set; } = string.Empty;
    public byte[]? BinaryContent { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import options
/// </summary>
public class ImportOptions
{
    public ImportSource Source { get; set; } = ImportSource.File;
    public string Content { get; set; } = string.Empty;
    public byte[]? BinaryContent { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public ImportFormat Format { get; set; } = ImportFormat.JSON;
    public string TenantId { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; } = false;
    public bool ValidateBeforeImport { get; set; } = true;
    public Dictionary<string, object> MappingRules { get; set; } = new();
}

/// <summary>
/// Import sources
/// </summary>
public enum ImportSource
{
    File,
    Url,
    Database,
    API,
    Template,
    Clipboard,
    Custom
}

/// <summary>
/// Import formats
/// </summary>
public enum ImportFormat
{
    JSON,
    XML,
    YAML,
    CSV,
    Excel,
    Custom
}

/// <summary>
/// Clone options
/// </summary>
public class CloneOptions
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public bool IncludeData { get; set; } = false;
    public bool IncludeVersionHistory { get; set; } = false;
    public bool IncludePermissions { get; set; } = false;
    public Dictionary<string, object> Overrides { get; set; } = new();
}

/// <summary>
/// Project version
/// </summary>
public class ProjectVersion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public VersionType Type { get; set; } = VersionType.Minor;
    public List<VersionChange> Changes { get; set; } = new();
    public bool IsActive { get; set; } = false;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Version types
/// </summary>
public enum VersionType
{
    Major,
    Minor,
    Patch,
    Hotfix,
    Beta,
    Alpha
}

/// <summary>
/// Version change
/// </summary>
public class VersionChange
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ElementId { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Project snapshot
/// </summary>
public class ProjectSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VisualConfigProject ProjectData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}