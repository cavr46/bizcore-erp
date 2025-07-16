using System.ComponentModel.DataAnnotations;

namespace BizCore.VisualConfig.Models;

/// <summary>
/// Visual configuration project
/// </summary>
public class VisualConfigProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public ConfigProjectType Type { get; set; } = ConfigProjectType.Workflow;
    public ConfigProjectStatus Status { get; set; } = ConfigProjectStatus.Draft;
    public string Version { get; set; } = "1.0.0";
    public VisualCanvas Canvas { get; set; } = new();
    public ConfigMetadata Metadata { get; set; } = new();
    public ConfigPermissions Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string? ParentProjectId { get; set; }
    public bool IsTemplate { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Configuration project types
/// </summary>
public enum ConfigProjectType
{
    Workflow,
    Form,
    Dashboard,
    Report,
    Integration,
    BusinessRule,
    DataModel,
    API,
    Automation,
    Template
}

/// <summary>
/// Configuration project status
/// </summary>
public enum ConfigProjectStatus
{
    Draft,
    InReview,
    Testing,
    Approved,
    Published,
    Archived,
    Deprecated
}

/// <summary>
/// Visual canvas for drag & drop design
/// </summary>
public class VisualCanvas
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public CanvasSettings Settings { get; set; } = new();
    public List<VisualElement> Elements { get; set; } = new();
    public List<VisualConnection> Connections { get; set; } = new();
    public CanvasLayout Layout { get; set; } = new();
    public CanvasViewport Viewport { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Canvas settings
/// </summary>
public class CanvasSettings
{
    public bool GridEnabled { get; set; } = true;
    public int GridSize { get; set; } = 20;
    public bool SnapToGrid { get; set; } = true;
    public bool ShowRulers { get; set; } = true;
    public double ZoomLevel { get; set; } = 1.0;
    public CanvasTheme Theme { get; set; } = CanvasTheme.Light;
    public bool ReadOnly { get; set; } = false;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Canvas theme
/// </summary>
public enum CanvasTheme
{
    Light,
    Dark,
    HighContrast,
    Custom
}

/// <summary>
/// Visual element (node) in canvas
/// </summary>
public class VisualElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ElementPosition Position { get; set; } = new();
    public ElementSize Size { get; set; } = new();
    public ElementStyle Style { get; set; } = new();
    public ElementConfiguration Configuration { get; set; } = new();
    public List<ElementPort> Ports { get; set; } = new();
    public ElementValidation Validation { get; set; } = new();
    public bool IsLocked { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public string ParentId { get; set; } = string.Empty;
    public List<string> ChildIds { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Element position
/// </summary>
public class ElementPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; } = 0;
    public double Rotation { get; set; } = 0;
}

/// <summary>
/// Element size
/// </summary>
public class ElementSize
{
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 50;
    public bool AutoSize { get; set; } = false;
    public double MinWidth { get; set; } = 50;
    public double MinHeight { get; set; } = 30;
    public double MaxWidth { get; set; } = 1000;
    public double MaxHeight { get; set; } = 1000;
}

/// <summary>
/// Element visual style
/// </summary>
public class ElementStyle
{
    public string BackgroundColor { get; set; } = "#ffffff";
    public string BorderColor { get; set; } = "#cccccc";
    public string TextColor { get; set; } = "#333333";
    public double BorderWidth { get; set; } = 1;
    public string BorderStyle { get; set; } = "solid";
    public double BorderRadius { get; set; } = 4;
    public string FontFamily { get; set; } = "Arial";
    public double FontSize { get; set; } = 12;
    public string FontWeight { get; set; } = "normal";
    public double Opacity { get; set; } = 1.0;
    public string Shadow { get; set; } = string.Empty;
    public Dictionary<string, string> CustomStyles { get; set; } = new();
}

/// <summary>
/// Element configuration
/// </summary>
public class ElementConfiguration
{
    public string ConfigType { get; set; } = string.Empty;
    public Dictionary<string, ConfigProperty> Properties { get; set; } = new();
    public List<ConfigEvent> Events { get; set; } = new();
    public List<ConfigAction> Actions { get; set; } = new();
    public List<ConfigValidation> Validations { get; set; } = new();
    public Dictionary<string, object> RuntimeData { get; set; } = new();
}

/// <summary>
/// Configuration property
/// </summary>
public class ConfigProperty
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ConfigPropertyType Type { get; set; } = ConfigPropertyType.Text;
    public object? Value { get; set; }
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public string Category { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
    public ConfigPropertyOptions Options { get; set; } = new();
    public List<ConfigPropertyValidation> Validations { get; set; } = new();
}

/// <summary>
/// Configuration property types
/// </summary>
public enum ConfigPropertyType
{
    Text,
    Number,
    Boolean,
    Date,
    DateTime,
    Email,
    Url,
    Phone,
    Color,
    File,
    Image,
    Dropdown,
    MultiSelect,
    RadioButton,
    Checkbox,
    Textarea,
    RichText,
    Code,
    JSON,
    Expression,
    Reference,
    Custom
}

/// <summary>
/// Configuration property options
/// </summary>
public class ConfigPropertyOptions
{
    public List<ConfigPropertyOption> Items { get; set; } = new();
    public string DataSource { get; set; } = string.Empty;
    public string ValueField { get; set; } = "value";
    public string DisplayField { get; set; } = "text";
    public bool AllowCustomValues { get; set; } = false;
    public bool MultiSelect { get; set; } = false;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Configuration property option
/// </summary>
public class ConfigPropertyOption
{
    public object Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Configuration property validation
/// </summary>
public class ConfigPropertyValidation
{
    public string Type { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Configuration event
/// </summary>
public class ConfigEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public List<ConfigAction> Actions { get; set; } = new();
    public ConfigCondition? Condition { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Configuration action
/// </summary>
public class ConfigAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ConfigCondition? Condition { get; set; }
    public int Order { get; set; }
    public bool IsAsync { get; set; } = false;
    public TimeSpan? Delay { get; set; }
}

/// <summary>
/// Configuration condition
/// </summary>
public class ConfigCondition
{
    public string Expression { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.And;
    public List<ConfigCondition> SubConditions { get; set; } = new();
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
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,
    Custom
}

/// <summary>
/// Configuration validation
/// </summary>
public class ConfigValidation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Validation severity
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Element port for connections
/// </summary>
public class ElementPort
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public PortType Type { get; set; } = PortType.Input;
    public PortDirection Direction { get; set; } = PortDirection.Left;
    public string DataType { get; set; } = "any";
    public bool IsRequired { get; set; } = false;
    public bool AllowMultiple { get; set; } = false;
    public PortPosition Position { get; set; } = new();
    public PortStyle Style { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Port types
/// </summary>
public enum PortType
{
    Input,
    Output,
    InputOutput
}

/// <summary>
/// Port directions
/// </summary>
public enum PortDirection
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// Port position
/// </summary>
public class PortPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsRelative { get; set; } = true;
}

/// <summary>
/// Port style
/// </summary>
public class PortStyle
{
    public string Color { get; set; } = "#007acc";
    public double Size { get; set; } = 8;
    public string Shape { get; set; } = "circle";
    public string BorderColor { get; set; } = "#ffffff";
    public double BorderWidth { get; set; } = 2;
}

/// <summary>
/// Element validation
/// </summary>
public class ElementValidation
{
    public bool IsValid { get; set; } = true;
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public DateTime LastValidated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Validation warning
/// </summary>
public class ValidationWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

/// <summary>
/// Visual connection between elements
/// </summary>
public class VisualConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = string.Empty;
    public ConnectionEndpoint Source { get; set; } = new();
    public ConnectionEndpoint Target { get; set; } = new();
    public ConnectionPath Path { get; set; } = new();
    public ConnectionStyle Style { get; set; } = new();
    public ConnectionData Data { get; set; } = new();
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Connection endpoint
/// </summary>
public class ConnectionEndpoint
{
    public string ElementId { get; set; } = string.Empty;
    public string PortId { get; set; } = string.Empty;
    public ConnectionAnchor Anchor { get; set; } = new();
}

/// <summary>
/// Connection anchor
/// </summary>
public class ConnectionAnchor
{
    public double X { get; set; }
    public double Y { get; set; }
    public AnchorType Type { get; set; } = AnchorType.Port;
    public AnchorDirection Direction { get; set; } = AnchorDirection.Auto;
}

/// <summary>
/// Anchor types
/// </summary>
public enum AnchorType
{
    Port,
    Element,
    Custom
}

/// <summary>
/// Anchor directions
/// </summary>
public enum AnchorDirection
{
    Auto,
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest
}

/// <summary>
/// Connection path
/// </summary>
public class ConnectionPath
{
    public PathType Type { get; set; } = PathType.Bezier;
    public List<PathPoint> Points { get; set; } = new();
    public double Curvature { get; set; } = 0.5;
    public bool AutoRoute { get; set; } = true;
    public PathRouting Routing { get; set; } = PathRouting.Orthogonal;
}

/// <summary>
/// Path types
/// </summary>
public enum PathType
{
    Straight,
    Bezier,
    Orthogonal,
    Curved,
    Custom
}

/// <summary>
/// Path routing
/// </summary>
public enum PathRouting
{
    Direct,
    Orthogonal,
    Bezier,
    Manhattan,
    Custom
}

/// <summary>
/// Path point
/// </summary>
public class PathPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public PathPointType Type { get; set; } = PathPointType.Line;
    public double? ControlX1 { get; set; }
    public double? ControlY1 { get; set; }
    public double? ControlX2 { get; set; }
    public double? ControlY2 { get; set; }
}

/// <summary>
/// Path point types
/// </summary>
public enum PathPointType
{
    Move,
    Line,
    Curve,
    Arc
}

/// <summary>
/// Connection style
/// </summary>
public class ConnectionStyle
{
    public string Color { get; set; } = "#666666";
    public double Width { get; set; } = 2;
    public string LineStyle { get; set; } = "solid";
    public string StartMarker { get; set; } = string.Empty;
    public string EndMarker { get; set; } = "arrow";
    public double Opacity { get; set; } = 1.0;
    public bool Animated { get; set; } = false;
    public string AnimationType { get; set; } = "flow";
    public Dictionary<string, string> CustomStyles { get; set; } = new();
}

/// <summary>
/// Connection data
/// </summary>
public class ConnectionData
{
    public string DataType { get; set; } = "any";
    public object? Value { get; set; }
    public bool IsConditional { get; set; } = false;
    public ConfigCondition? Condition { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Canvas layout
/// </summary>
public class CanvasLayout
{
    public LayoutType Type { get; set; } = LayoutType.Free;
    public LayoutDirection Direction { get; set; } = LayoutDirection.TopToBottom;
    public double Spacing { get; set; } = 50;
    public LayoutAlignment Alignment { get; set; } = LayoutAlignment.Center;
    public bool AutoLayout { get; set; } = false;
    public Dictionary<string, object> CustomLayout { get; set; } = new();
}

/// <summary>
/// Layout types
/// </summary>
public enum LayoutType
{
    Free,
    Grid,
    Hierarchical,
    Circular,
    Force,
    Custom
}

/// <summary>
/// Layout directions
/// </summary>
public enum LayoutDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft
}

/// <summary>
/// Layout alignment
/// </summary>
public enum LayoutAlignment
{
    Start,
    Center,
    End,
    Stretch
}

/// <summary>
/// Canvas viewport
/// </summary>
public class CanvasViewport
{
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double Zoom { get; set; } = 1.0;
    public double MinZoom { get; set; } = 0.1;
    public double MaxZoom { get; set; } = 5.0;
    public bool PanEnabled { get; set; } = true;
    public bool ZoomEnabled { get; set; } = true;
}

/// <summary>
/// Configuration metadata
/// </summary>
public class ConfigMetadata
{
    public string Category { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Process { get; set; } = string.Empty;
    public string BusinessUnit { get; set; } = string.Empty;
    public ConfigComplexity Complexity { get; set; } = ConfigComplexity.Simple;
    public List<string> Keywords { get; set; } = new();
    public List<ConfigDependency> Dependencies { get; set; } = new();
    public ConfigDocumentation Documentation { get; set; } = new();
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Configuration complexity
/// </summary>
public enum ConfigComplexity
{
    Simple,
    Medium,
    Complex,
    Advanced
}

/// <summary>
/// Configuration dependency
/// </summary>
public class ConfigDependency
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DependencyType Type { get; set; } = DependencyType.Required;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Dependency types
/// </summary>
public enum DependencyType
{
    Required,
    Optional,
    Recommended,
    Conflict
}

/// <summary>
/// Configuration documentation
/// </summary>
public class ConfigDocumentation
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserGuide { get; set; } = string.Empty;
    public string TechnicalNotes { get; set; } = string.Empty;
    public List<ConfigExample> Examples { get; set; } = new();
    public List<ConfigFAQ> FAQ { get; set; } = new();
    public string ChangeLog { get; set; } = string.Empty;
}

/// <summary>
/// Configuration example
/// </summary>
public class ConfigExample
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = "json";
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Configuration FAQ
/// </summary>
public class ConfigFAQ
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// Configuration permissions
/// </summary>
public class ConfigPermissions
{
    public string Owner { get; set; } = string.Empty;
    public List<ConfigPermissionEntry> Entries { get; set; } = new();
    public bool IsPublic { get; set; } = false;
    public bool AllowFork { get; set; } = true;
    public bool AllowExport { get; set; } = true;
    public string License { get; set; } = string.Empty;
}

/// <summary>
/// Configuration permission entry
/// </summary>
public class ConfigPermissionEntry
{
    public string PrincipalId { get; set; } = string.Empty;
    public PrincipalType PrincipalType { get; set; } = PrincipalType.User;
    public List<ConfigPermissionType> Permissions { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Principal types
/// </summary>
public enum PrincipalType
{
    User,
    Role,
    Group,
    Team,
    Organization
}

/// <summary>
/// Configuration permission types
/// </summary>
public enum ConfigPermissionType
{
    View,
    Edit,
    Delete,
    Execute,
    Deploy,
    Share,
    Admin
}

/// <summary>
/// Visual config request DTOs
/// </summary>
public class CreateVisualConfigRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public ConfigProjectType Type { get; set; } = ConfigProjectType.Workflow;
    public bool IsTemplate { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public ConfigMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Visual config query parameters
/// </summary>
public class VisualConfigQuery
{
    public string? TenantId { get; set; }
    public ConfigProjectType? Type { get; set; }
    public ConfigProjectStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string? Category { get; set; }
    public ConfigComplexity? Complexity { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string SortBy { get; set; } = "UpdatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Visual config result wrapper
/// </summary>
public class VisualConfigResult
{
    public bool IsSuccess { get; set; }
    public VisualConfigProject? Project { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static VisualConfigResult Success(VisualConfigProject project) =>
        new() { IsSuccess = true, Project = project };

    public static VisualConfigResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static VisualConfigResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}