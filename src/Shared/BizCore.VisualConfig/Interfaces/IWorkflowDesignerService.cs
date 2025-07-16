using BizCore.VisualConfig.Models;

namespace BizCore.VisualConfig.Interfaces;

/// <summary>
/// Workflow designer service interface
/// </summary>
public interface IWorkflowDesignerService
{
    /// <summary>
    /// Create workflow definition
    /// </summary>
    Task<WorkflowResult> CreateWorkflowAsync(CreateWorkflowRequest request);

    /// <summary>
    /// Update workflow definition
    /// </summary>
    Task<WorkflowResult> UpdateWorkflowAsync(string workflowId, WorkflowDefinition workflow);

    /// <summary>
    /// Get workflow definition
    /// </summary>
    Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId);

    /// <summary>
    /// Execute workflow
    /// </summary>
    Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, WorkflowContext context);

    /// <summary>
    /// Validate workflow definition
    /// </summary>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(string workflowId);

    /// <summary>
    /// Get workflow execution history
    /// </summary>
    Task<IEnumerable<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int count = 50);

    /// <summary>
    /// Cancel workflow execution
    /// </summary>
    Task<bool> CancelExecutionAsync(string executionId);

    /// <summary>
    /// Get workflow templates
    /// </summary>
    Task<IEnumerable<WorkflowTemplate>> GetTemplatesAsync(string category = "");

    /// <summary>
    /// Create workflow from template
    /// </summary>
    Task<WorkflowResult> CreateFromTemplateAsync(string templateId, CreateFromTemplateRequest request);

    /// <summary>
    /// Get workflow metrics
    /// </summary>
    Task<WorkflowMetrics> GetMetricsAsync(string workflowId, DateTime? fromDate = null, DateTime? toDate = null);
}

/// <summary>
/// Workflow definition
/// </summary>
public class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<WorkflowTransition> Transitions { get; set; } = new();
    public WorkflowConfiguration Configuration { get; set; } = new();
    public WorkflowTrigger Trigger { get; set; } = new();
    public List<WorkflowVariable> Variables { get; set; } = new();
    public WorkflowSecurity Security { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Workflow status
/// </summary>
public enum WorkflowStatus
{
    Draft,
    Published,
    Active,
    Suspended,
    Deprecated,
    Archived
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
    public ElementPosition Position { get; set; } = new();
    public WorkflowStepConfiguration Configuration { get; set; } = new();
    public List<WorkflowAction> Actions { get; set; } = new();
    public List<WorkflowCondition> Conditions { get; set; } = new();
    public WorkflowStepTimeout Timeout { get; set; } = new();
    public WorkflowStepRetry Retry { get; set; } = new();
    public bool IsStartStep { get; set; } = false;
    public bool IsEndStep { get; set; } = false;
    public Dictionary<string, object> Data { get; set; } = new();
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
    Gateway,
    Event,
    Custom
}

/// <summary>
/// Workflow step configuration
/// </summary>
public class WorkflowStepConfiguration
{
    public string Handler { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public WorkflowStepAssignment Assignment { get; set; } = new();
    public WorkflowStepEscalation Escalation { get; set; } = new();
    public List<WorkflowStepValidator> Validators { get; set; } = new();
    public Dictionary<string, string> InputMapping { get; set; } = new();
    public Dictionary<string, string> OutputMapping { get; set; } = new();
}

/// <summary>
/// Workflow step assignment
/// </summary>
public class WorkflowStepAssignment
{
    public AssignmentType Type { get; set; } = AssignmentType.User;
    public string Assignee { get; set; } = string.Empty;
    public List<string> Candidates { get; set; } = new();
    public string AssignmentRule { get; set; } = string.Empty;
    public bool AllowReassignment { get; set; } = true;
    public bool AllowDelegation { get; set; } = true;
}

/// <summary>
/// Assignment types
/// </summary>
public enum AssignmentType
{
    User,
    Role,
    Group,
    Expression,
    Service,
    Auto
}

/// <summary>
/// Workflow step escalation
/// </summary>
public class WorkflowStepEscalation
{
    public bool IsEnabled { get; set; } = false;
    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(24);
    public string EscalateTo { get; set; } = string.Empty;
    public List<WorkflowAction> Actions { get; set; } = new();
    public bool RepeatEscalation { get; set; } = false;
    public TimeSpan RepeatInterval { get; set; } = TimeSpan.FromHours(12);
}

/// <summary>
/// Workflow step validator
/// </summary>
public class WorkflowStepValidator
{
    public string Type { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Workflow step timeout
/// </summary>
public class WorkflowStepTimeout
{
    public bool IsEnabled { get; set; } = false;
    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
    public TimeoutAction Action { get; set; } = TimeoutAction.Cancel;
    public string NextStepId { get; set; } = string.Empty;
    public List<WorkflowAction> TimeoutActions { get; set; } = new();
}

/// <summary>
/// Timeout actions
/// </summary>
public enum TimeoutAction
{
    Cancel,
    Complete,
    Escalate,
    Retry,
    Skip,
    GoToStep
}

/// <summary>
/// Workflow step retry
/// </summary>
public class WorkflowStepRetry
{
    public bool IsEnabled { get; set; } = false;
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public RetryStrategy Strategy { get; set; } = RetryStrategy.Linear;
    public List<string> RetryOnErrors { get; set; } = new();
    public List<string> DoNotRetryOnErrors { get; set; } = new();
}

/// <summary>
/// Retry strategies
/// </summary>
public enum RetryStrategy
{
    Linear,
    Exponential,
    Fixed,
    Custom
}

/// <summary>
/// Workflow transition
/// </summary>
public class WorkflowTransition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string FromStepId { get; set; } = string.Empty;
    public string ToStepId { get; set; } = string.Empty;
    public WorkflowCondition? Condition { get; set; }
    public TransitionType Type { get; set; } = TransitionType.Sequence;
    public int Priority { get; set; } = 0;
    public List<WorkflowAction> Actions { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Transition types
/// </summary>
public enum TransitionType
{
    Sequence,
    Conditional,
    Default,
    Exception,
    Timer,
    Message,
    Signal
}

/// <summary>
/// Workflow condition
/// </summary>
public class WorkflowCondition
{
    public string Expression { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.And;
    public List<WorkflowCondition> SubConditions { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
    public string Script { get; set; } = string.Empty;
    public string ScriptLanguage { get; set; } = "javascript";
}

/// <summary>
/// Workflow action
/// </summary>
public class WorkflowAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public WorkflowCondition? Condition { get; set; }
    public int Order { get; set; }
    public bool IsAsync { get; set; } = false;
    public TimeSpan? Delay { get; set; }
    public WorkflowActionRetry Retry { get; set; } = new();
}

/// <summary>
/// Workflow action retry
/// </summary>
public class WorkflowActionRetry
{
    public bool IsEnabled { get; set; } = false;
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public RetryStrategy Strategy { get; set; } = RetryStrategy.Linear;
}

/// <summary>
/// Workflow configuration
/// </summary>
public class WorkflowConfiguration
{
    public bool IsParallelExecutionEnabled { get; set; } = false;
    public int MaxConcurrentExecutions { get; set; } = 10;
    public bool EnableAuditTrail { get; set; } = true;
    public bool EnableVersioning { get; set; } = true;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromHours(24);
    public WorkflowPersistence Persistence { get; set; } = new();
    public WorkflowNotification Notification { get; set; } = new();
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Workflow persistence
/// </summary>
public class WorkflowPersistence
{
    public bool IsEnabled { get; set; } = true;
    public PersistenceStrategy Strategy { get; set; } = PersistenceStrategy.Always;
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(90);
    public bool CompressData { get; set; } = true;
    public bool EncryptData { get; set; } = false;
}

/// <summary>
/// Persistence strategies
/// </summary>
public enum PersistenceStrategy
{
    Always,
    OnCompletion,
    OnError,
    Never,
    Custom
}

/// <summary>
/// Workflow notification
/// </summary>
public class WorkflowNotification
{
    public bool IsEnabled { get; set; } = true;
    public List<NotificationEvent> Events { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
    public string Template { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
}

/// <summary>
/// Notification events
/// </summary>
public enum NotificationEvent
{
    Started,
    Completed,
    Failed,
    Suspended,
    Resumed,
    StepCompleted,
    StepFailed,
    Escalated,
    Timeout
}

/// <summary>
/// Notification channels
/// </summary>
public enum NotificationChannel
{
    Email,
    SMS,
    Push,
    InApp,
    Webhook,
    Slack,
    Teams
}

/// <summary>
/// Workflow trigger
/// </summary>
public class WorkflowTrigger
{
    public TriggerType Type { get; set; } = TriggerType.Manual;
    public TriggerConfiguration Configuration { get; set; } = new();
    public WorkflowCondition? Condition { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Schedule { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
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
    EmailReceived,
    Custom
}

/// <summary>
/// Trigger configuration
/// </summary>
public class TriggerConfiguration
{
    public string EventSource { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> EventFilters { get; set; } = new();
    public bool BatchProcessing { get; set; } = false;
    public int BatchSize { get; set; } = 100;
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Workflow variable
/// </summary>
public class WorkflowVariable
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public WorkflowVariableType Type { get; set; } = WorkflowVariableType.String;
    public object? Value { get; set; }
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public VariableScope Scope { get; set; } = VariableScope.Workflow;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Workflow variable types
/// </summary>
public enum WorkflowVariableType
{
    String,
    Number,
    Boolean,
    Date,
    DateTime,
    Array,
    Object,
    File,
    Reference
}

/// <summary>
/// Variable scopes
/// </summary>
public enum VariableScope
{
    Workflow,
    Step,
    Global,
    Session,
    Request
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
    public Dictionary<string, object> SecurityPolicies { get; set; } = new();
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
/// Workflow execution
/// </summary>
public class WorkflowExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowVersion { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public WorkflowContext Context { get; set; } = new();
    public List<WorkflowStepExecution> StepExecutions { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public WorkflowExecutionMetrics Metrics { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Workflow execution status
/// </summary>
public enum WorkflowExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Suspended,
    Timeout
}

/// <summary>
/// Workflow context
/// </summary>
public class WorkflowContext
{
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string BusinessKey { get; set; } = string.Empty;
    public Dictionary<string, object> Variables { get; set; } = new();
    public Dictionary<string, object> ProcessData { get; set; } = new();
    public string CorrelationId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workflow step execution
/// </summary>
public class WorkflowStepExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public WorkflowStepExecutionStatus Status { get; set; } = WorkflowStepExecutionStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? AssignedTo { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> InputData { get; set; } = new();
    public Dictionary<string, object> OutputData { get; set; } = new();
    public List<WorkflowActionExecution> ActionExecutions { get; set; } = new();
}

/// <summary>
/// Workflow step execution status
/// </summary>
public enum WorkflowStepExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Waiting,
    Timeout
}

/// <summary>
/// Workflow action execution
/// </summary>
public class WorkflowActionExecution
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public WorkflowActionExecutionStatus Status { get; set; } = WorkflowActionExecutionStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Result { get; set; } = new();
}

/// <summary>
/// Workflow action execution status
/// </summary>
public enum WorkflowActionExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Retrying
}

/// <summary>
/// Workflow execution metrics
/// </summary>
public class WorkflowExecutionMetrics
{
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan ActiveDuration { get; set; }
    public TimeSpan WaitingDuration { get; set; }
    public int StepsCompleted { get; set; }
    public int StepsFailed { get; set; }
    public int ActionsExecuted { get; set; }
    public long MemoryUsed { get; set; }
    public double CpuUsed { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Workflow template
/// </summary>
public class WorkflowTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public WorkflowDefinition Definition { get; set; } = new();
    public List<TemplateParameter> Parameters { get; set; } = new();
    public string PreviewUrl { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public double Rating { get; set; }
    public bool IsOfficial { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Template parameter
/// </summary>
public class TemplateParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public string Description { get; set; } = string.Empty;
    public List<string> AllowedValues { get; set; } = new();
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
    public TimeSpan FastestExecution { get; set; }
    public TimeSpan SlowestExecution { get; set; }
    public int ActiveExecutions { get; set; }
    public Dictionary<string, int> StepMetrics { get; set; } = new();
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Workflow validation result
/// </summary>
public class WorkflowValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workflow result wrapper
/// </summary>
public class WorkflowResult
{
    public bool IsSuccess { get; set; }
    public WorkflowDefinition? Workflow { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static WorkflowResult Success(WorkflowDefinition workflow) =>
        new() { IsSuccess = true, Workflow = workflow };

    public static WorkflowResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static WorkflowResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}

/// <summary>
/// Create workflow request
/// </summary>
public class CreateWorkflowRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<WorkflowTransition> Transitions { get; set; } = new();
    public WorkflowTrigger Trigger { get; set; } = new();
}

/// <summary>
/// Create from template request
/// </summary>
public class CreateFromTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public Dictionary<string, object> Parameters { get; set; } = new();
}