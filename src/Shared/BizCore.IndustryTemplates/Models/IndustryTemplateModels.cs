using System.ComponentModel.DataAnnotations;

namespace BizCore.IndustryTemplates.Models;

/// <summary>
/// Industry template model for sector-specific ERP configurations
/// </summary>
public class IndustryTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IndustryType Industry { get; set; } = IndustryType.General;
    public string IndustryCode { get; set; } = string.Empty;
    public TemplateSize Size { get; set; } = TemplateSize.Medium;
    public TemplateComplexity Complexity { get; set; } = TemplateComplexity.Standard;
    public string Version { get; set; } = "1.0.0";
    public TemplateStatus Status { get; set; } = TemplateStatus.Active;
    
    // Template content
    public TemplateConfiguration Configuration { get; set; } = new();
    public List<TemplateModule> Modules { get; set; } = new();
    public List<TemplateWorkflow> Workflows { get; set; } = new();
    public List<TemplateReport> Reports { get; set; } = new();
    public List<TemplateDashboard> Dashboards { get; set; } = new();
    public List<TemplateIntegration> Integrations { get; set; } = new();
    public TemplateCustomization Customization { get; set; } = new();
    
    // Deployment
    public TemplateDeployment Deployment { get; set; } = new();
    public List<TemplateValidation> Validations { get; set; } = new();
    public TemplateMetrics Metrics { get; set; } = new();
    
    // Metadata
    public TemplateMetadata Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    
    // Localization
    public List<TemplateLocalization> Localizations { get; set; } = new();
    public string DefaultLanguage { get; set; } = "en";
    public List<string> SupportedCountries { get; set; } = new();
    
    // Pricing and licensing
    public TemplatePricing Pricing { get; set; } = new();
    public TemplateLicense License { get; set; } = new();
    
    // Support and documentation
    public TemplateSupport Support { get; set; } = new();
    public List<TemplateDocumentation> Documentation { get; set; } = new();
    public List<TemplateResource> Resources { get; set; } = new();
    
    // Lifecycle
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
    
    // Usage statistics
    public TemplateUsageStats UsageStats { get; set; } = new();
}

/// <summary>
/// Industry types supported by the template system
/// </summary>
public enum IndustryType
{
    General,
    Retail,
    Manufacturing,
    Healthcare,
    Financial,
    RealEstate,
    Construction,
    Agriculture,
    Technology,
    Education,
    Hospitality,
    Transportation,
    Energy,
    Government,
    NonProfit,
    Professional,
    Automotive,
    Pharmaceutical,
    Food,
    Textile,
    Mining,
    Telecommunications,
    Media,
    Insurance,
    Legal,
    Consulting,
    Logistics,
    Aviation,
    Maritime,
    Defense
}

/// <summary>
/// Template size categories
/// </summary>
public enum TemplateSize
{
    Micro,      // 1-5 employees
    Small,      // 6-25 employees
    Medium,     // 26-100 employees
    Large,      // 100-500 employees
    Enterprise  // 500+ employees
}

/// <summary>
/// Template complexity levels
/// </summary>
public enum TemplateComplexity
{
    Basic,
    Standard,
    Advanced,
    Expert,
    Custom
}

/// <summary>
/// Template status
/// </summary>
public enum TemplateStatus
{
    Draft,
    Review,
    Active,
    Deprecated,
    Archived,
    Beta,
    Maintenance
}

/// <summary>
/// Template configuration
/// </summary>
public class TemplateConfiguration
{
    public string TenantSettings { get; set; } = string.Empty;
    public Dictionary<string, object> GlobalSettings { get; set; } = new();
    public List<ConfigurationSection> Sections { get; set; } = new();
    public List<ConfigurationVariable> Variables { get; set; } = new();
    public List<ConfigurationRule> Rules { get; set; } = new();
    public ConfigurationSecurity Security { get; set; } = new();
    public Dictionary<string, object> FeatureFlags { get; set; } = new();
    public List<ConfigurationEnvironment> Environments { get; set; } = new();
}

/// <summary>
/// Configuration section
/// </summary>
public class ConfigurationSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public string Category { get; set; } = string.Empty;
    public List<ConfigurationField> Fields { get; set; } = new();
    public Dictionary<string, object> DefaultValues { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Configuration field
/// </summary>
public class ConfigurationField
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FieldType Type { get; set; } = FieldType.Text;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadonly { get; set; } = false;
    public string ValidationRule { get; set; } = string.Empty;
    public List<FieldOption> Options { get; set; } = new();
    public Dictionary<string, object> Attributes { get; set; } = new();
    public string HelpText { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
}

/// <summary>
/// Field types
/// </summary>
public enum FieldType
{
    Text,
    Number,
    Boolean,
    Date,
    DateTime,
    Email,
    Phone,
    Url,
    Password,
    TextArea,
    Select,
    MultiSelect,
    Checkbox,
    Radio,
    File,
    Image,
    Json,
    Code,
    Color,
    Range,
    Custom
}

/// <summary>
/// Field option for select/radio fields
/// </summary>
public class FieldOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public bool IsDisabled { get; set; } = false;
    public string Group { get; set; } = string.Empty;
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Configuration variable
/// </summary>
public class ConfigurationVariable
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VariableType Type { get; set; } = VariableType.String;
    public object? Value { get; set; }
    public object? DefaultValue { get; set; }
    public VariableScope Scope { get; set; } = VariableScope.Template;
    public bool IsSecret { get; set; } = false;
    public bool IsEnvironmentSpecific { get; set; } = false;
    public string Category { get; set; } = string.Empty;
    public List<string> AllowedValues { get; set; } = new();
    public string ValidationPattern { get; set; } = string.Empty;
}

/// <summary>
/// Variable types
/// </summary>
public enum VariableType
{
    String,
    Number,
    Boolean,
    Array,
    Object,
    Secret
}

/// <summary>
/// Variable scope
/// </summary>
public enum VariableScope
{
    Global,
    Template,
    Module,
    Workflow,
    User,
    Tenant
}

/// <summary>
/// Configuration rule
/// </summary>
public class ConfigurationRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RuleType Type { get; set; } = RuleType.Validation;
    public string Expression { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Error;
    public bool IsEnabled { get; set; } = true;
    public List<string> DependsOn { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Rule types
/// </summary>
public enum RuleType
{
    Validation,
    Transformation,
    Conditional,
    Security,
    Business,
    Compliance
}

/// <summary>
/// Rule severity levels
/// </summary>
public enum RuleSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Configuration security settings
/// </summary>
public class ConfigurationSecurity
{
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public bool RequireApproval { get; set; } = false;
    public string ApprovalWorkflow { get; set; } = string.Empty;
    public bool AuditChanges { get; set; } = true;
    public bool EncryptSensitiveData { get; set; } = true;
    public List<string> SensitiveFields { get; set; } = new();
    public Dictionary<string, object> SecurityPolicies { get; set; } = new();
}

/// <summary>
/// Configuration environment
/// </summary>
public class ConfigurationEnvironment
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EnvironmentType Type { get; set; } = EnvironmentType.Development;
    public bool IsDefault { get; set; } = false;
    public Dictionary<string, object> Settings { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public List<string> AllowedRoles { get; set; } = new();
}

/// <summary>
/// Environment types
/// </summary>
public enum EnvironmentType
{
    Development,
    Testing,
    Staging,
    Production,
    Sandbox,
    Demo
}

/// <summary>
/// Template module
/// </summary>
public class TemplateModule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ModuleType Type { get; set; } = ModuleType.Core;
    public string Version { get; set; } = "1.0.0";
    public bool IsRequired { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public List<string> Conflicts { get; set; } = new();
    public ModuleConfiguration Configuration { get; set; } = new();
    public List<ModulePermission> Permissions { get; set; } = new();
    public List<ModuleResource> Resources { get; set; } = new();
    public ModuleLifecycle Lifecycle { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Module types
/// </summary>
public enum ModuleType
{
    Core,
    Business,
    Integration,
    Reporting,
    Workflow,
    Security,
    Analytics,
    Communication,
    Storage,
    Custom
}

/// <summary>
/// Module configuration
/// </summary>
public class ModuleConfiguration
{
    public bool AutoInstall { get; set; } = true;
    public bool AutoUpdate { get; set; } = false;
    public bool AllowUninstall { get; set; } = true;
    public string InstallScript { get; set; } = string.Empty;
    public string UninstallScript { get; set; } = string.Empty;
    public string UpdateScript { get; set; } = string.Empty;
    public Dictionary<string, object> InstallParameters { get; set; } = new();
    public List<string> PrerequisiteChecks { get; set; } = new();
    public List<string> PostInstallActions { get; set; } = new();
}

/// <summary>
/// Module permission
/// </summary>
public class ModulePermission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PermissionType Type { get; set; } = PermissionType.Read;
    public string Resource { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Users { get; set; } = new();
    public bool IsRequired { get; set; } = false;
    public Dictionary<string, object> Constraints { get; set; } = new();
}

/// <summary>
/// Permission types
/// </summary>
public enum PermissionType
{
    Read,
    Write,
    Delete,
    Execute,
    Admin,
    Custom
}

/// <summary>
/// Module resource
/// </summary>
public class ModuleResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Module lifecycle
/// </summary>
public class ModuleLifecycle
{
    public DateTime? InstalledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? UninstalledAt { get; set; }
    public string InstalledBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public string InstalledVersion { get; set; } = string.Empty;
    public string PreviousVersion { get; set; } = string.Empty;
    public ModuleStatus Status { get; set; } = ModuleStatus.Available;
    public List<string> InstallLog { get; set; } = new();
    public List<string> ErrorLog { get; set; } = new();
}

/// <summary>
/// Module status
/// </summary>
public enum ModuleStatus
{
    Available,
    Installing,
    Installed,
    Failed,
    Updating,
    Updated,
    Uninstalling,
    Uninstalled,
    Disabled,
    Error
}

/// <summary>
/// Template workflow
/// </summary>
public class TemplateWorkflow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowType Type { get; set; } = WorkflowType.Business;
    public string Category { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0;
    public WorkflowDefinition Definition { get; set; } = new();
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<WorkflowTrigger> Triggers { get; set; } = new();
    public WorkflowSecurity Security { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
    public WorkflowMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Workflow types
/// </summary>
public enum WorkflowType
{
    Business,
    Approval,
    Notification,
    Integration,
    Maintenance,
    Security,
    Compliance,
    Custom
}

/// <summary>
/// Workflow definition
/// </summary>
public class WorkflowDefinition
{
    public string Version { get; set; } = "1.0.0";
    public string Schema { get; set; } = string.Empty;
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<string> InputParameters { get; set; } = new();
    public List<string> OutputParameters { get; set; } = new();
    public TimeSpan? Timeout { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string ErrorHandling { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Workflow step
/// </summary>
public class WorkflowStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowStepType Type { get; set; } = WorkflowStepType.Task;
    public int Order { get; set; }
    public bool IsRequired { get; set; } = true;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<WorkflowCondition> Conditions { get; set; } = new();
    public List<string> NextSteps { get; set; } = new();
    public WorkflowStepConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Workflow step types
/// </summary>
public enum WorkflowStepType
{
    Start,
    End,
    Task,
    Decision,
    Parallel,
    Loop,
    SubWorkflow,
    UserTask,
    ServiceTask,
    ScriptTask,
    EmailTask,
    TimerTask,
    Custom
}

/// <summary>
/// Workflow condition
/// </summary>
public class WorkflowCondition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Expression { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.And;
    public List<WorkflowCondition> SubConditions { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Condition operators
/// </summary>
public enum ConditionOperator
{
    And,
    Or,
    Not,
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    Contains,
    StartsWith,
    EndsWith
}

/// <summary>
/// Workflow step configuration
/// </summary>
public class WorkflowStepConfiguration
{
    public TimeSpan? Timeout { get; set; }
    public int MaxRetries { get; set; } = 0;
    public TimeSpan? RetryDelay { get; set; }
    public bool SkipOnError { get; set; } = false;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Workflow trigger
/// </summary>
public class WorkflowTrigger
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TriggerType Type { get; set; } = TriggerType.Manual;
    public bool IsEnabled { get; set; } = true;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Conditions { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
    public TriggerSchedule? Schedule { get; set; }
}

/// <summary>
/// Trigger types
/// </summary>
public enum TriggerType
{
    Manual,
    Scheduled,
    Event,
    API,
    Webhook,
    FileChange,
    DatabaseChange,
    Custom
}

/// <summary>
/// Trigger schedule
/// </summary>
public class TriggerSchedule
{
    public string CronExpression { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public bool IsRecurring { get; set; } = false;
    public int MaxExecutions { get; set; } = -1;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Workflow security
/// </summary>
public class WorkflowSecurity
{
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public bool RequireAuthorization { get; set; } = true;
    public SecurityLevel Level { get; set; } = SecurityLevel.Standard;
    public bool AuditExecution { get; set; } = true;
    public bool EncryptData { get; set; } = false;
}

/// <summary>
/// Security levels
/// </summary>
public enum SecurityLevel
{
    Public,
    Standard,
    Restricted,
    Confidential,
    TopSecret
}

/// <summary>
/// Workflow metrics
/// </summary>
public class WorkflowMetrics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? LastSuccessfulExecution { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Template report
/// </summary>
public class TemplateReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReportType Type { get; set; } = ReportType.Tabular;
    public string Category { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public ReportDefinition Definition { get; set; } = new();
    public List<ReportParameter> Parameters { get; set; } = new();
    public ReportSecurity Security { get; set; } = new();
    public ReportSchedule? Schedule { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Report types
/// </summary>
public enum ReportType
{
    Tabular,
    Chart,
    Dashboard,
    Matrix,
    Subreport,
    Custom
}

/// <summary>
/// Report definition
/// </summary>
public class ReportDefinition
{
    public string Query { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public List<ReportField> Fields { get; set; } = new();
    public List<ReportGroup> Groups { get; set; } = new();
    public List<ReportSort> Sorts { get; set; } = new();
    public List<ReportFilter> Filters { get; set; } = new();
    public ReportLayout Layout { get; set; } = new();
    public ReportFormatting Formatting { get; set; } = new();
}

/// <summary>
/// Report field
/// </summary>
public class ReportField
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public FieldDataType DataType { get; set; } = FieldDataType.String;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    public string Format { get; set; } = string.Empty;
    public ReportAggregation? Aggregation { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Field data types
/// </summary>
public enum FieldDataType
{
    String,
    Number,
    Date,
    DateTime,
    Boolean,
    Currency,
    Percentage,
    Image,
    Url,
    Custom
}

/// <summary>
/// Report aggregation
/// </summary>
public class ReportAggregation
{
    public AggregationType Type { get; set; } = AggregationType.Sum;
    public string Expression { get; set; } = string.Empty;
    public bool ShowInGroup { get; set; } = true;
    public bool ShowInTotal { get; set; } = true;
}

/// <summary>
/// Aggregation types
/// </summary>
public enum AggregationType
{
    Sum,
    Count,
    Average,
    Min,
    Max,
    Distinct,
    Custom
}

/// <summary>
/// Report group
/// </summary>
public class ReportGroup
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowHeader { get; set; } = true;
    public bool ShowFooter { get; set; } = true;
    public bool KeepTogether { get; set; } = false;
    public string HeaderTemplate { get; set; } = string.Empty;
    public string FooterTemplate { get; set; } = string.Empty;
}

/// <summary>
/// Report sort
/// </summary>
public class ReportSort
{
    public string FieldName { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
    public int Order { get; set; }
}

/// <summary>
/// Sort directions
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Report filter
/// </summary>
public class ReportFilter
{
    public string FieldName { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;
    public object? Value { get; set; }
    public object? Value2 { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public string DisplayName { get; set; } = string.Empty;
    public FilterDataType DataType { get; set; } = FilterDataType.String;
}

/// <summary>
/// Filter operators
/// </summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Between,
    In,
    NotIn,
    Like,
    NotLike,
    IsNull,
    IsNotNull,
    StartsWith,
    EndsWith,
    Contains,
    NotContains
}

/// <summary>
/// Filter data types
/// </summary>
public enum FilterDataType
{
    String,
    Number,
    Date,
    DateTime,
    Boolean,
    List,
    Custom
}

/// <summary>
/// Report layout
/// </summary>
public class ReportLayout
{
    public LayoutOrientation Orientation { get; set; } = LayoutOrientation.Portrait;
    public PageSize PageSize { get; set; } = PageSize.A4;
    public ReportMargins Margins { get; set; } = new();
    public string HeaderTemplate { get; set; } = string.Empty;
    public string FooterTemplate { get; set; } = string.Empty;
    public bool ShowPageNumbers { get; set; } = true;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Layout orientations
/// </summary>
public enum LayoutOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Page sizes
/// </summary>
public enum PageSize
{
    A4,
    A3,
    A5,
    Letter,
    Legal,
    Tabloid,
    Custom
}

/// <summary>
/// Report margins
/// </summary>
public class ReportMargins
{
    public double Top { get; set; } = 1.0;
    public double Bottom { get; set; } = 1.0;
    public double Left { get; set; } = 1.0;
    public double Right { get; set; } = 1.0;
    public string Unit { get; set; } = "inch";
}

/// <summary>
/// Report formatting
/// </summary>
public class ReportFormatting
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 10;
    public string Theme { get; set; } = "Default";
    public Dictionary<string, object> Colors { get; set; } = new();
    public Dictionary<string, object> Styles { get; set; } = new();
    public bool AlternateRowColors { get; set; } = true;
    public Dictionary<string, object> CustomFormatting { get; set; } = new();
}

/// <summary>
/// Report parameter
/// </summary>
public class ReportParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ParameterDataType DataType { get; set; } = ParameterDataType.String;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public bool AllowMultiple { get; set; } = false;
    public List<ParameterOption> Options { get; set; } = new();
    public string ValidationRule { get; set; } = string.Empty;
}

/// <summary>
/// Parameter data types
/// </summary>
public enum ParameterDataType
{
    String,
    Number,
    Date,
    DateTime,
    Boolean,
    List,
    Custom
}

/// <summary>
/// Parameter option
/// </summary>
public class ParameterOption
{
    public object Value { get; set; } = new();
    public string Label { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public string Group { get; set; } = string.Empty;
}

/// <summary>
/// Report security
/// </summary>
public class ReportSecurity
{
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public bool RequireAuthorization { get; set; } = true;
    public SecurityLevel Level { get; set; } = SecurityLevel.Standard;
    public bool AuditAccess { get; set; } = true;
    public List<string> RestrictedFields { get; set; } = new();
}

/// <summary>
/// Report schedule
/// </summary>
public class ReportSchedule
{
    public bool IsEnabled { get; set; } = false;
    public string CronExpression { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public List<string> Recipients { get; set; } = new();
    public ReportDeliveryMethod DeliveryMethod { get; set; } = ReportDeliveryMethod.Email;
    public ReportOutputFormat OutputFormat { get; set; } = ReportOutputFormat.PDF;
    public Dictionary<string, object> DeliveryOptions { get; set; } = new();
}

/// <summary>
/// Report delivery methods
/// </summary>
public enum ReportDeliveryMethod
{
    Email,
    FileShare,
    FTP,
    API,
    Database,
    Custom
}

/// <summary>
/// Report output formats
/// </summary>
public enum ReportOutputFormat
{
    PDF,
    Excel,
    CSV,
    JSON,
    XML,
    HTML,
    Word,
    PowerPoint,
    Custom
}

/// <summary>
/// Template dashboard
/// </summary>
public class TemplateDashboard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public List<DashboardWidget> Widgets { get; set; } = new();
    public DashboardLayout Layout { get; set; } = new();
    public DashboardSecurity Security { get; set; } = new();
    public DashboardConfiguration Configuration { get; set; } = new();
    public List<DashboardFilter> Filters { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Dashboard widget
/// </summary>
public class DashboardWidget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WidgetType Type { get; set; } = WidgetType.Chart;
    public WidgetPosition Position { get; set; } = new();
    public WidgetSize Size { get; set; } = new();
    public WidgetDataSource DataSource { get; set; } = new();
    public WidgetConfiguration Configuration { get; set; } = new();
    public WidgetSecurity Security { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Widget types
/// </summary>
public enum WidgetType
{
    Chart,
    Table,
    Card,
    Gauge,
    Map,
    Text,
    Image,
    Video,
    IFrame,
    Custom
}

/// <summary>
/// Widget position
/// </summary>
public class WidgetPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; } = 0;
}

/// <summary>
/// Widget size
/// </summary>
public class WidgetSize
{
    public int Width { get; set; } = 4;
    public int Height { get; set; } = 3;
    public int MinWidth { get; set; } = 1;
    public int MinHeight { get; set; } = 1;
    public int MaxWidth { get; set; } = 12;
    public int MaxHeight { get; set; } = 12;
    public bool IsResizable { get; set; } = true;
}

/// <summary>
/// Widget data source
/// </summary>
public class WidgetDataSource
{
    public string Type { get; set; } = string.Empty;
    public string Connection { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableRealTime { get; set; } = false;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Widget configuration
/// </summary>
public class WidgetConfiguration
{
    public string ChartType { get; set; } = string.Empty;
    public Dictionary<string, object> ChartOptions { get; set; } = new();
    public Dictionary<string, string> ColorScheme { get; set; } = new();
    public bool ShowLegend { get; set; } = true;
    public bool ShowTooltip { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string NumberFormat { get; set; } = "#,##0.00";
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Widget security
/// </summary>
public class WidgetSecurity
{
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public SecurityLevel Level { get; set; } = SecurityLevel.Standard;
    public List<string> RestrictedData { get; set; } = new();
}

/// <summary>
/// Dashboard layout
/// </summary>
public class DashboardLayout
{
    public LayoutType Type { get; set; } = LayoutType.Grid;
    public int Columns { get; set; } = 12;
    public int RowHeight { get; set; } = 100;
    public bool IsDraggable { get; set; } = true;
    public bool IsResizable { get; set; } = true;
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Layout types
/// </summary>
public enum LayoutType
{
    Grid,
    Flex,
    Fixed,
    Custom
}

/// <summary>
/// Dashboard security
/// </summary>
public class DashboardSecurity
{
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public bool RequireAuthorization { get; set; } = true;
    public SecurityLevel Level { get; set; } = SecurityLevel.Standard;
    public bool AuditAccess { get; set; } = true;
    public bool AllowExport { get; set; } = true;
    public bool AllowShare { get; set; } = true;
}

/// <summary>
/// Dashboard configuration
/// </summary>
public class DashboardConfiguration
{
    public TimeSpan AutoRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool ShowTitle { get; set; } = true;
    public bool ShowFilters { get; set; } = true;
    public bool ShowRefreshButton { get; set; } = true;
    public bool ShowExportButton { get; set; } = true;
    public bool ShowFullscreenButton { get; set; } = true;
    public string Theme { get; set; } = "light";
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Dashboard filter
/// </summary>
public class DashboardFilter
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public FilterType Type { get; set; } = FilterType.Text;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public List<string> AffectedWidgets { get; set; } = new();
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Filter types
/// </summary>
public enum FilterType
{
    Text,
    Number,
    Date,
    DateRange,
    Select,
    MultiSelect,
    Boolean,
    Custom
}

/// <summary>
/// Template integration
/// </summary>
public class TemplateIntegration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IntegrationType Type { get; set; } = IntegrationType.API;
    public string Category { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public IntegrationConfiguration Configuration { get; set; } = new();
    public IntegrationSecurity Security { get; set; } = new();
    public List<IntegrationEndpoint> Endpoints { get; set; } = new();
    public List<IntegrationMapping> Mappings { get; set; } = new();
    public IntegrationMonitoring Monitoring { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Integration types
/// </summary>
public enum IntegrationType
{
    API,
    Database,
    FileSystem,
    FTP,
    Email,
    Webhook,
    MessageQueue,
    Custom
}

/// <summary>
/// Integration configuration
/// </summary>
public class IntegrationConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = string.Empty;
    public Dictionary<string, object> AuthenticationSettings { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableLogging { get; set; } = true;
    public bool EnableCaching { get; set; } = false;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Integration security
/// </summary>
public class IntegrationSecurity
{
    public string AuthenticationType { get; set; } = string.Empty;
    public Dictionary<string, object> Credentials { get; set; } = new();
    public bool UseSSL { get; set; } = true;
    public bool ValidateCertificate { get; set; } = true;
    public List<string> AllowedIPs { get; set; } = new();
    public List<string> BlockedIPs { get; set; } = new();
    public bool EnableRateLimit { get; set; } = false;
    public int RateLimitRequests { get; set; } = 100;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Integration endpoint
/// </summary>
public class IntegrationEndpoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, object> Headers { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string RequestTemplate { get; set; } = string.Empty;
    public string ResponseTemplate { get; set; } = string.Empty;
    public EndpointSecurity Security { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Endpoint security
/// </summary>
public class EndpointSecurity
{
    public bool RequireAuthentication { get; set; } = true;
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool EnableRateLimit { get; set; } = false;
    public int RateLimitRequests { get; set; } = 100;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Integration mapping
/// </summary>
public class IntegrationMapping
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public MappingType Type { get; set; } = MappingType.Direct;
    public string TransformExpression { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Mapping types
/// </summary>
public enum MappingType
{
    Direct,
    Transform,
    Lookup,
    Calculate,
    Conditional,
    Custom
}

/// <summary>
/// Integration monitoring
/// </summary>
public class IntegrationMonitoring
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthCheck { get; set; } = true;
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableAlerting { get; set; } = true;
    public List<MonitoringAlert> Alerts { get; set; } = new();
    public Dictionary<string, object> Thresholds { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Monitoring alert
/// </summary>
public class MonitoringAlert
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public bool IsEnabled { get; set; } = true;
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Template customization settings
/// </summary>
public class TemplateCustomization
{
    public bool AllowCustomization { get; set; } = true;
    public List<CustomizationScope> AllowedScopes { get; set; } = new();
    public List<string> ProtectedFields { get; set; } = new();
    public List<string> RequiredFields { get; set; } = new();
    public CustomizationSecurity Security { get; set; } = new();
    public List<CustomizationRule> Rules { get; set; } = new();
    public Dictionary<string, object> Limits { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Customization scopes
/// </summary>
public enum CustomizationScope
{
    Fields,
    Workflows,
    Reports,
    Dashboards,
    Integrations,
    Security,
    UI,
    Business
}

/// <summary>
/// Customization security
/// </summary>
public class CustomizationSecurity
{
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireApproval { get; set; } = false;
    public string ApprovalWorkflow { get; set; } = string.Empty;
    public bool AuditChanges { get; set; } = true;
    public bool EnableVersioning { get; set; } = true;
    public int MaxVersions { get; set; } = 10;
}

/// <summary>
/// Customization rule
/// </summary>
public class CustomizationRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Error;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Template deployment settings
/// </summary>
public class TemplateDeployment
{
    public DeploymentStrategy Strategy { get; set; } = DeploymentStrategy.Standard;
    public List<DeploymentStep> Steps { get; set; } = new();
    public DeploymentConfiguration Configuration { get; set; } = new();
    public DeploymentSecurity Security { get; set; } = new();
    public DeploymentMonitoring Monitoring { get; set; } = new();
    public DeploymentRollback Rollback { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Deployment strategies
/// </summary>
public enum DeploymentStrategy
{
    Standard,
    BlueGreen,
    Canary,
    RollingUpdate,
    Staged,
    Custom
}

/// <summary>
/// Deployment step
/// </summary>
public class DeploymentStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeploymentStepType Type { get; set; } = DeploymentStepType.Script;
    public int Order { get; set; }
    public bool IsRequired { get; set; } = true;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> Prerequisites { get; set; } = new();
    public DeploymentStepConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Deployment step types
/// </summary>
public enum DeploymentStepType
{
    Script,
    Database,
    FileSystem,
    Configuration,
    Service,
    Validation,
    Notification,
    Custom
}

/// <summary>
/// Deployment step configuration
/// </summary>
public class DeploymentStepConfiguration
{
    public TimeSpan? Timeout { get; set; }
    public int MaxRetries { get; set; } = 0;
    public TimeSpan? RetryDelay { get; set; }
    public bool SkipOnError { get; set; } = false;
    public bool RunInParallel { get; set; } = false;
    public Dictionary<string, object> Environment { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Deployment configuration
/// </summary>
public class DeploymentConfiguration
{
    public bool CreateBackup { get; set; } = true;
    public bool ValidateBeforeDeployment { get; set; } = true;
    public bool ValidateAfterDeployment { get; set; } = true;
    public bool NotifyUsers { get; set; } = false;
    public List<string> NotificationRecipients { get; set; } = new();
    public TimeSpan? MaintenanceWindow { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Deployment security
/// </summary>
public class DeploymentSecurity
{
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool RequireApproval { get; set; } = true;
    public string ApprovalWorkflow { get; set; } = string.Empty;
    public bool RequireMultipleApprovals { get; set; } = false;
    public int MinimumApprovals { get; set; } = 1;
    public bool AuditDeployment { get; set; } = true;
    public SecurityLevel Level { get; set; } = SecurityLevel.Standard;
}

/// <summary>
/// Deployment monitoring
/// </summary>
public class DeploymentMonitoring
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthCheck { get; set; } = true;
    public bool EnableAlerting { get; set; } = true;
    public List<MonitoringAlert> Alerts { get; set; } = new();
    public Dictionary<string, object> Thresholds { get; set; } = new();
    public TimeSpan MonitoringDuration { get; set; } = TimeSpan.FromHours(1);
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Deployment rollback settings
/// </summary>
public class DeploymentRollback
{
    public bool EnableAutoRollback { get; set; } = false;
    public List<string> RollbackTriggers { get; set; } = new();
    public TimeSpan RollbackTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool CreateRollbackPlan { get; set; } = true;
    public bool TestRollbackPlan { get; set; } = false;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Template validation
/// </summary>
public class TemplateValidation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ValidationType Type { get; set; } = ValidationType.Schema;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    public bool IsEnabled { get; set; } = true;
    public string Expression { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ValidationConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Validation types
/// </summary>
public enum ValidationType
{
    Schema,
    Business,
    Security,
    Performance,
    Compliance,
    Compatibility,
    Custom
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Validation configuration
/// </summary>
public class ValidationConfiguration
{
    public bool RunOnCreate { get; set; } = true;
    public bool RunOnUpdate { get; set; } = true;
    public bool RunOnDeploy { get; set; } = true;
    public bool StopOnFailure { get; set; } = true;
    public TimeSpan? Timeout { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Template metrics
/// </summary>
public class TemplateMetrics
{
    public int TotalInstallations { get; set; }
    public int ActiveInstallations { get; set; }
    public int SuccessfulDeployments { get; set; }
    public int FailedDeployments { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageDeploymentTime { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public DateTime? LastDeployment { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Template metadata
/// </summary>
public class TemplateMetadata
{
    public string Author { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public List<string> Contributors { get; set; } = new();
    public List<TemplateReference> References { get; set; } = new();
    public List<TemplateDependency> Dependencies { get; set; } = new();
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Template reference
/// </summary>
public class TemplateReference
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Template dependency
/// </summary>
public class TemplateDependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Template localization
/// </summary>
public class TemplateLocalization
{
    public string Language { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Translations { get; set; } = new();
    public Dictionary<string, object> LocalizedSettings { get; set; } = new();
    public bool IsComplete { get; set; } = false;
    public DateTime? LastUpdated { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Template pricing
/// </summary>
public class TemplatePricing
{
    public PricingModel Model { get; set; } = PricingModel.Free;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public PricingFrequency Frequency { get; set; } = PricingFrequency.OneTime;
    public bool HasFreeTrial { get; set; } = false;
    public int FreeTrialDays { get; set; } = 0;
    public List<PricingTier> Tiers { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Pricing models
/// </summary>
public enum PricingModel
{
    Free,
    OneTime,
    Subscription,
    Usage,
    Tiered,
    Custom
}

/// <summary>
/// Pricing frequencies
/// </summary>
public enum PricingFrequency
{
    OneTime,
    Monthly,
    Quarterly,
    Yearly,
    Usage,
    Custom
}

/// <summary>
/// Pricing tier
/// </summary>
public class PricingTier
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MinUsers { get; set; }
    public int MaxUsers { get; set; }
    public List<string> Features { get; set; } = new();
    public Dictionary<string, object> Limits { get; set; } = new();
    public bool IsPopular { get; set; } = false;
}

/// <summary>
/// Template license
/// </summary>
public class TemplateLicense
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public List<string> Limitations { get; set; } = new();
    public List<string> Conditions { get; set; } = new();
    public Dictionary<string, object> Terms { get; set; } = new();
}

/// <summary>
/// Template support
/// </summary>
public class TemplateSupport
{
    public SupportLevel Level { get; set; } = SupportLevel.Community;
    public string ContactEmail { get; set; } = string.Empty;
    public string SupportUrl { get; set; } = string.Empty;
    public string ForumUrl { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
    public List<SupportChannel> Channels { get; set; } = new();
    public SupportAvailability Availability { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Support levels
/// </summary>
public enum SupportLevel
{
    None,
    Community,
    Basic,
    Standard,
    Premium,
    Enterprise,
    Custom
}

/// <summary>
/// Support channel
/// </summary>
public class SupportChannel
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public SupportAvailability Availability { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Support availability
/// </summary>
public class SupportAvailability
{
    public string TimeZone { get; set; } = "UTC";
    public List<SupportHours> Hours { get; set; } = new();
    public List<string> Holidays { get; set; } = new();
    public string ResponseTime { get; set; } = string.Empty;
    public string ResolutionTime { get; set; } = string.Empty;
}

/// <summary>
/// Support hours
/// </summary>
public class SupportHours
{
    public DayOfWeek Day { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// Template documentation
/// </summary>
public class TemplateDocumentation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DocumentationType Type { get; set; } = DocumentationType.Guide;
    public string Category { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Language { get; set; } = "en";
    public DocumentationFormat Format { get; set; } = DocumentationFormat.Markdown;
    public List<string> Tags { get; set; } = new();
    public DocumentationMetadata Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Documentation types
/// </summary>
public enum DocumentationType
{
    Guide,
    Tutorial,
    Reference,
    FAQ,
    Troubleshooting,
    API,
    Configuration,
    Installation,
    Upgrade,
    Custom
}

/// <summary>
/// Documentation formats
/// </summary>
public enum DocumentationFormat
{
    Markdown,
    HTML,
    PDF,
    Video,
    Interactive,
    Custom
}

/// <summary>
/// Documentation metadata
/// </summary>
public class DocumentationMetadata
{
    public string Author { get; set; } = string.Empty;
    public List<string> Contributors { get; set; } = new();
    public int ViewCount { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public DateTime? LastReviewed { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Template resource
/// </summary>
public class TemplateResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ResourceType Type { get; set; } = ResourceType.File;
    public string Url { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsDownloadable { get; set; } = true;
    public ResourceMetadata Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Resource types
/// </summary>
public enum ResourceType
{
    File,
    Image,
    Video,
    Audio,
    Document,
    Archive,
    Script,
    Configuration,
    Database,
    Custom
}

/// <summary>
/// Resource metadata
/// </summary>
public class ResourceMetadata
{
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public DateTime? LastDownloaded { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Template usage statistics
/// </summary>
public class TemplateUsageStats
{
    public int TotalDownloads { get; set; }
    public int TotalInstalls { get; set; }
    public int ActiveInstalls { get; set; }
    public int TotalUninstalls { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastUsed { get; set; }
    public Dictionary<string, int> UsageByCountry { get; set; } = new();
    public Dictionary<string, int> UsageByIndustry { get; set; } = new();
    public Dictionary<string, int> UsageBySize { get; set; } = new();
    public Dictionary<string, object> CustomStats { get; set; } = new();
}

/// <summary>
/// Request models
/// </summary>
public class CreateIndustryTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public IndustryType Industry { get; set; }
    
    public TemplateSize Size { get; set; } = TemplateSize.Medium;
    
    public TemplateComplexity Complexity { get; set; } = TemplateComplexity.Standard;
    
    public List<string> Tags { get; set; } = new();
    
    public string Category { get; set; } = string.Empty;
    
    public List<string> SupportedCountries { get; set; } = new();
    
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Industry template result wrapper
/// </summary>
public class IndustryTemplateResult
{
    public bool IsSuccess { get; set; }
    public IndustryTemplate? Template { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public List<ValidationWarning> ValidationWarnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static IndustryTemplateResult Success(IndustryTemplate template) =>
        new() { IsSuccess = true, Template = template };

    public static IndustryTemplateResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static IndustryTemplateResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}

/// <summary>
/// Validation error for industry templates
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
/// Validation warning for industry templates
/// </summary>
public class ValidationWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}