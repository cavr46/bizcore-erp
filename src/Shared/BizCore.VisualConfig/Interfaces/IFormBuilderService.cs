using BizCore.VisualConfig.Models;
using System.ComponentModel.DataAnnotations;

namespace BizCore.VisualConfig.Interfaces;

/// <summary>
/// Form builder service interface
/// </summary>
public interface IFormBuilderService
{
    /// <summary>
    /// Create form definition
    /// </summary>
    Task<FormResult> CreateFormAsync(CreateFormRequest request);

    /// <summary>
    /// Update form definition
    /// </summary>
    Task<FormResult> UpdateFormAsync(string formId, FormDefinition form);

    /// <summary>
    /// Get form definition
    /// </summary>
    Task<FormDefinition?> GetFormAsync(string formId);

    /// <summary>
    /// Query form definitions
    /// </summary>
    Task<IEnumerable<FormDefinition>> QueryFormsAsync(FormQuery query);

    /// <summary>
    /// Delete form definition
    /// </summary>
    Task<bool> DeleteFormAsync(string formId);

    /// <summary>
    /// Validate form definition
    /// </summary>
    Task<FormValidationResult> ValidateFormAsync(string formId);

    /// <summary>
    /// Generate form code
    /// </summary>
    Task<FormCodeResult> GenerateFormCodeAsync(string formId, CodeGenerationOptions options);

    /// <summary>
    /// Submit form data
    /// </summary>
    Task<FormSubmissionResult> SubmitFormAsync(string formId, FormSubmission submission);

    /// <summary>
    /// Get form submissions
    /// </summary>
    Task<IEnumerable<FormSubmission>> GetSubmissionsAsync(string formId, int skip = 0, int take = 50);

    /// <summary>
    /// Export form definition
    /// </summary>
    Task<ExportResult> ExportFormAsync(string formId, ExportOptions options);

    /// <summary>
    /// Import form definition
    /// </summary>
    Task<FormResult> ImportFormAsync(ImportOptions options);

    /// <summary>
    /// Get form templates
    /// </summary>
    Task<IEnumerable<FormTemplate>> GetTemplatesAsync(string category = "");

    /// <summary>
    /// Create form from template
    /// </summary>
    Task<FormResult> CreateFromTemplateAsync(string templateId, CreateFormFromTemplateRequest request);

    /// <summary>
    /// Preview form
    /// </summary>
    Task<FormPreview> PreviewFormAsync(string formId, PreviewOptions options);

    /// <summary>
    /// Validate form submission
    /// </summary>
    Task<FormValidationResult> ValidateSubmissionAsync(string formId, Dictionary<string, object> data);
}

/// <summary>
/// Form definition
/// </summary>
public class FormDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public FormStatus Status { get; set; } = FormStatus.Draft;
    public List<FormSection> Sections { get; set; } = new();
    public FormLayout Layout { get; set; } = new();
    public FormStyling Styling { get; set; } = new();
    public FormConfiguration Configuration { get; set; } = new();
    public FormValidationRules ValidationRules { get; set; } = new();
    public FormSecurity Security { get; set; } = new();
    public FormSubmissionSettings SubmissionSettings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsTemplate { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Form status
/// </summary>
public enum FormStatus
{
    Draft,
    Review,
    Published,
    Active,
    Inactive,
    Archived
}

/// <summary>
/// Form section
/// </summary>
public class FormSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsCollapsible { get; set; } = false;
    public bool IsCollapsed { get; set; } = false;
    public List<FormField> Fields { get; set; } = new();
    public FormSectionLayout Layout { get; set; } = new();
    public FormSectionStyling Styling { get; set; } = new();
    public FormVisibilityRule? VisibilityRule { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Form field
/// </summary>
public class FormField
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
    public FormFieldType Type { get; set; } = FormFieldType.Text;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    public FormFieldLayout Layout { get; set; } = new();
    public FormFieldStyling Styling { get; set; } = new();
    public FormFieldConfiguration Configuration { get; set; } = new();
    public List<FormFieldValidation> Validations { get; set; } = new();
    public List<FormFieldOption> Options { get; set; } = new();
    public FormVisibilityRule? VisibilityRule { get; set; }
    public FormCalculationRule? CalculationRule { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Form field types
/// </summary>
public enum FormFieldType
{
    Text,
    TextArea,
    RichText,
    Number,
    Decimal,
    Currency,
    Percentage,
    Email,
    Phone,
    Url,
    Password,
    Date,
    DateTime,
    Time,
    Boolean,
    Checkbox,
    Radio,
    Dropdown,
    MultiSelect,
    AutoComplete,
    File,
    Image,
    Signature,
    Rating,
    Slider,
    Color,
    Hidden,
    Calculated,
    Display,
    Separator,
    Repeater,
    SubForm,
    DataGrid,
    Address,
    Location,
    Barcode,
    QRCode,
    Custom
}

/// <summary>
/// Form field configuration
/// </summary>
public class FormFieldConfiguration
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public string? Pattern { get; set; }
    public string? Mask { get; set; }
    public string? Format { get; set; }
    public bool AllowMultiple { get; set; } = false;
    public string? AcceptedFileTypes { get; set; }
    public long? MaxFileSize { get; set; }
    public string? DataSource { get; set; }
    public string? ValueField { get; set; }
    public string? DisplayField { get; set; }
    public bool AutoSave { get; set; } = false;
    public string? AutoCompleteUrl { get; set; }
    public int? DecimalPlaces { get; set; }
    public string? CurrencySymbol { get; set; }
    public Dictionary<string, object> CustomConfiguration { get; set; } = new();
}

/// <summary>
/// Form field validation
/// </summary>
public class FormFieldValidation
{
    public string Type { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    public bool IsClientSide { get; set; } = true;
    public bool IsServerSide { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Form field option
/// </summary>
public class FormFieldOption
{
    public object Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = false;
    public bool IsDisabled { get; set; } = false;
    public string Color { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Form visibility rule
/// </summary>
public class FormVisibilityRule
{
    public string Condition { get; set; } = string.Empty;
    public List<FormVisibilityCondition> Conditions { get; set; } = new();
    public VisibilityOperator Operator { get; set; } = VisibilityOperator.And;
    public VisibilityAction Action { get; set; } = VisibilityAction.Show;
    public string? Animation { get; set; }
}

/// <summary>
/// Form visibility condition
/// </summary>
public class FormVisibilityCondition
{
    public string FieldName { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;
    public object? Value { get; set; }
    public string? Function { get; set; }
}

/// <summary>
/// Visibility operators
/// </summary>
public enum VisibilityOperator
{
    And,
    Or,
    Not
}

/// <summary>
/// Visibility actions
/// </summary>
public enum VisibilityAction
{
    Show,
    Hide,
    Enable,
    Disable,
    Require,
    Optional
}

/// <summary>
/// Form calculation rule
/// </summary>
public class FormCalculationRule
{
    public string Expression { get; set; } = string.Empty;
    public List<string> DependentFields { get; set; } = new();
    public CalculationType Type { get; set; } = CalculationType.Formula;
    public string? Function { get; set; }
    public bool RecalculateOnChange { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Calculation types
/// </summary>
public enum CalculationType
{
    Formula,
    Sum,
    Average,
    Count,
    Min,
    Max,
    Concatenate,
    Lookup,
    Custom
}

/// <summary>
/// Form layout
/// </summary>
public class FormLayout
{
    public FormLayoutType Type { get; set; } = FormLayoutType.Vertical;
    public int Columns { get; set; } = 1;
    public string Spacing { get; set; } = "medium";
    public bool IsResponsive { get; set; } = true;
    public FormBreakpoints Breakpoints { get; set; } = new();
    public string Width { get; set; } = "100%";
    public string MaxWidth { get; set; } = "800px";
    public string Alignment { get; set; } = "left";
    public Dictionary<string, object> CustomLayout { get; set; } = new();
}

/// <summary>
/// Form layout types
/// </summary>
public enum FormLayoutType
{
    Vertical,
    Horizontal,
    Grid,
    Tabs,
    Accordion,
    Wizard,
    Card,
    Custom
}

/// <summary>
/// Form breakpoints
/// </summary>
public class FormBreakpoints
{
    public int Mobile { get; set; } = 1;
    public int Tablet { get; set; } = 2;
    public int Desktop { get; set; } = 3;
    public int Large { get; set; } = 4;
}

/// <summary>
/// Form section layout
/// </summary>
public class FormSectionLayout
{
    public int Columns { get; set; } = 1;
    public string Spacing { get; set; } = "medium";
    public FormSectionLayoutType Type { get; set; } = FormSectionLayoutType.Grid;
    public bool ShowBorder { get; set; } = false;
    public string Padding { get; set; } = "medium";
    public Dictionary<string, object> CustomLayout { get; set; } = new();
}

/// <summary>
/// Form section layout types
/// </summary>
public enum FormSectionLayoutType
{
    Grid,
    Flex,
    Table,
    Custom
}

/// <summary>
/// Form field layout
/// </summary>
public class FormFieldLayout
{
    public int ColumnSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;
    public string Width { get; set; } = "auto";
    public string Height { get; set; } = "auto";
    public string Margin { get; set; } = string.Empty;
    public string Padding { get; set; } = string.Empty;
    public FormFieldAlignment Alignment { get; set; } = FormFieldAlignment.Left;
    public FormLabelPosition LabelPosition { get; set; } = FormLabelPosition.Top;
    public string LabelWidth { get; set; } = "auto";
    public Dictionary<string, object> CustomLayout { get; set; } = new();
}

/// <summary>
/// Form field alignment
/// </summary>
public enum FormFieldAlignment
{
    Left,
    Center,
    Right,
    Justify
}

/// <summary>
/// Form label positions
/// </summary>
public enum FormLabelPosition
{
    Top,
    Left,
    Right,
    Bottom,
    Hidden,
    Floating
}

/// <summary>
/// Form styling
/// </summary>
public class FormStyling
{
    public string Theme { get; set; } = "default";
    public FormColorScheme ColorScheme { get; set; } = new();
    public FormTypography Typography { get; set; } = new();
    public FormBorders Borders { get; set; } = new();
    public FormShadows Shadows { get; set; } = new();
    public string BackgroundColor { get; set; } = "#ffffff";
    public string BackgroundImage { get; set; } = string.Empty;
    public string CustomCSS { get; set; } = string.Empty;
    public Dictionary<string, object> CustomStyles { get; set; } = new();
}

/// <summary>
/// Form color scheme
/// </summary>
public class FormColorScheme
{
    public string Primary { get; set; } = "#007acc";
    public string Secondary { get; set; } = "#6c757d";
    public string Success { get; set; } = "#28a745";
    public string Warning { get; set; } = "#ffc107";
    public string Error { get; set; } = "#dc3545";
    public string Info { get; set; } = "#17a2b8";
    public string Light { get; set; } = "#f8f9fa";
    public string Dark { get; set; } = "#343a40";
}

/// <summary>
/// Form typography
/// </summary>
public class FormTypography
{
    public string FontFamily { get; set; } = "Arial, sans-serif";
    public string FontSize { get; set; } = "14px";
    public string FontWeight { get; set; } = "normal";
    public string LineHeight { get; set; } = "1.5";
    public string LetterSpacing { get; set; } = "normal";
    public string TextColor { get; set; } = "#333333";
    public FormTypographyElements Elements { get; set; } = new();
}

/// <summary>
/// Form typography elements
/// </summary>
public class FormTypographyElements
{
    public FormTypographyStyle Headings { get; set; } = new();
    public FormTypographyStyle Labels { get; set; } = new();
    public FormTypographyStyle Inputs { get; set; } = new();
    public FormTypographyStyle Buttons { get; set; } = new();
    public FormTypographyStyle HelpText { get; set; } = new();
    public FormTypographyStyle ErrorText { get; set; } = new();
}

/// <summary>
/// Form typography style
/// </summary>
public class FormTypographyStyle
{
    public string FontFamily { get; set; } = string.Empty;
    public string FontSize { get; set; } = string.Empty;
    public string FontWeight { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string LineHeight { get; set; } = string.Empty;
    public string LetterSpacing { get; set; } = string.Empty;
    public string TextTransform { get; set; } = string.Empty;
}

/// <summary>
/// Form borders
/// </summary>
public class FormBorders
{
    public string DefaultBorder { get; set; } = "1px solid #cccccc";
    public string FocusBorder { get; set; } = "1px solid #007acc";
    public string ErrorBorder { get; set; } = "1px solid #dc3545";
    public string BorderRadius { get; set; } = "4px";
    public FormBorderElements Elements { get; set; } = new();
}

/// <summary>
/// Form border elements
/// </summary>
public class FormBorderElements
{
    public string Form { get; set; } = string.Empty;
    public string Sections { get; set; } = string.Empty;
    public string Fields { get; set; } = string.Empty;
    public string Buttons { get; set; } = string.Empty;
}

/// <summary>
/// Form shadows
/// </summary>
public class FormShadows
{
    public string DefaultShadow { get; set; } = "none";
    public string FocusShadow { get; set; } = "0 0 0 0.2rem rgba(0, 123, 255, 0.25)";
    public string HoverShadow { get; set; } = "none";
    public FormShadowElements Elements { get; set; } = new();
}

/// <summary>
/// Form shadow elements
/// </summary>
public class FormShadowElements
{
    public string Form { get; set; } = string.Empty;
    public string Sections { get; set; } = string.Empty;
    public string Fields { get; set; } = string.Empty;
    public string Buttons { get; set; } = string.Empty;
}

/// <summary>
/// Form section styling
/// </summary>
public class FormSectionStyling
{
    public string BackgroundColor { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
    public string BorderWidth { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public string Padding { get; set; } = string.Empty;
    public string Margin { get; set; } = string.Empty;
    public string Shadow { get; set; } = string.Empty;
    public Dictionary<string, string> CustomStyles { get; set; } = new();
}

/// <summary>
/// Form field styling
/// </summary>
public class FormFieldStyling
{
    public string BackgroundColor { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string LabelColor { get; set; } = string.Empty;
    public string HelpTextColor { get; set; } = string.Empty;
    public string ErrorTextColor { get; set; } = string.Empty;
    public string PlaceholderColor { get; set; } = string.Empty;
    public string FontSize { get; set; } = string.Empty;
    public string FontWeight { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public string Padding { get; set; } = string.Empty;
    public string Margin { get; set; } = string.Empty;
    public Dictionary<string, string> CustomStyles { get; set; } = new();
}

/// <summary>
/// Form configuration
/// </summary>
public class FormConfiguration
{
    public bool EnableAutoSave { get; set; } = false;
    public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableProgressBar { get; set; } = false;
    public bool ShowRequiredIndicator { get; set; } = true;
    public string RequiredIndicator { get; set; } = "*";
    public bool EnableClientValidation { get; set; } = true;
    public bool EnableServerValidation { get; set; } = true;
    public bool ShowValidationSummary { get; set; } = true;
    public FormValidationDisplayMode ValidationDisplayMode { get; set; } = FormValidationDisplayMode.Inline;
    public bool EnableConditionalLogic { get; set; } = true;
    public bool EnableCalculations { get; set; } = true;
    public bool AllowDraft { get; set; } = true;
    public bool EnableSpellCheck { get; set; } = true;
    public bool EnableAutocomplete { get; set; } = true;
    public Dictionary<string, object> CustomConfiguration { get; set; } = new();
}

/// <summary>
/// Form validation display modes
/// </summary>
public enum FormValidationDisplayMode
{
    None,
    Inline,
    Summary,
    Both,
    Tooltip,
    Modal
}

/// <summary>
/// Form validation rules
/// </summary>
public class FormValidationRules
{
    public List<FormGlobalValidation> GlobalValidations { get; set; } = new();
    public List<FormCrossFieldValidation> CrossFieldValidations { get; set; } = new();
    public bool StopOnFirstError { get; set; } = false;
    public bool ValidateOnChange { get; set; } = true;
    public bool ValidateOnBlur { get; set; } = true;
    public bool ValidateOnSubmit { get; set; } = true;
    public string CustomValidationScript { get; set; } = string.Empty;
}

/// <summary>
/// Form global validation
/// </summary>
public class FormGlobalValidation
{
    public string Name { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Form cross field validation
/// </summary>
public class FormCrossFieldValidation
{
    public string Name { get; set; } = string.Empty;
    public List<string> Fields { get; set; } = new();
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Form security
/// </summary>
public class FormSecurity
{
    public bool RequireAuthentication { get; set; } = true;
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public bool EnableCaptcha { get; set; } = false;
    public string CaptchaProvider { get; set; } = string.Empty;
    public bool EnableRateLimiting { get; set; } = false;
    public int MaxSubmissionsPerHour { get; set; } = 100;
    public bool EnableCSRFProtection { get; set; } = true;
    public bool EnableXSSProtection { get; set; } = true;
    public bool EnableEncryption { get; set; } = false;
    public bool EnableDigitalSignature { get; set; } = false;
    public FormAccessControl AccessControl { get; set; } = new();
}

/// <summary>
/// Form access control
/// </summary>
public class FormAccessControl
{
    public FormAccessLevel ReadAccess { get; set; } = FormAccessLevel.Authenticated;
    public FormAccessLevel WriteAccess { get; set; } = FormAccessLevel.Authenticated;
    public FormAccessLevel DeleteAccess { get; set; } = FormAccessLevel.Owner;
    public bool AllowAnonymousSubmission { get; set; } = false;
    public bool AllowGuestSubmission { get; set; } = false;
    public TimeSpan? SubmissionWindow { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Form access levels
/// </summary>
public enum FormAccessLevel
{
    Public,
    Guest,
    Authenticated,
    Role,
    Owner,
    Admin
}

/// <summary>
/// Form submission settings
/// </summary>
public class FormSubmissionSettings
{
    public bool AllowMultipleSubmissions { get; set; } = false;
    public bool RequireConfirmation { get; set; } = false;
    public string ConfirmationMessage { get; set; } = "Thank you for your submission!";
    public string ConfirmationRedirectUrl { get; set; } = string.Empty;
    public bool SendConfirmationEmail { get; set; } = false;
    public string ConfirmationEmailTemplate { get; set; } = string.Empty;
    public List<FormNotificationRecipient> NotificationRecipients { get; set; } = new();
    public FormDataStorage DataStorage { get; set; } = new();
    public List<FormWorkflowTrigger> WorkflowTriggers { get; set; } = new();
    public List<FormIntegration> Integrations { get; set; } = new();
}

/// <summary>
/// Form notification recipient
/// </summary>
public class FormNotificationRecipient
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FormNotificationType Type { get; set; } = FormNotificationType.Email;
    public string Template { get; set; } = string.Empty;
    public bool IncludeSubmissionData { get; set; } = true;
    public FormCondition? Condition { get; set; }
}

/// <summary>
/// Form notification types
/// </summary>
public enum FormNotificationType
{
    Email,
    SMS,
    Webhook,
    Slack,
    Teams
}

/// <summary>
/// Form condition
/// </summary>
public class FormCondition
{
    public string Expression { get; set; } = string.Empty;
    public List<FormConditionRule> Rules { get; set; } = new();
    public ConditionOperator Operator { get; set; } = ConditionOperator.And;
}

/// <summary>
/// Form condition rule
/// </summary>
public class FormConditionRule
{
    public string FieldName { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;
    public object? Value { get; set; }
}

/// <summary>
/// Form data storage
/// </summary>
public class FormDataStorage
{
    public FormStorageType Type { get; set; } = FormStorageType.Database;
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public bool EnableEncryption { get; set; } = false;
    public bool EnableCompression { get; set; } = false;
    public TimeSpan? RetentionPeriod { get; set; }
    public bool EnableArchiving { get; set; } = false;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Form storage types
/// </summary>
public enum FormStorageType
{
    Database,
    File,
    Cloud,
    External,
    Memory
}

/// <summary>
/// Form workflow trigger
/// </summary>
public class FormWorkflowTrigger
{
    public string WorkflowId { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = "OnSubmit";
    public FormCondition? Condition { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsAsync { get; set; } = true;
}

/// <summary>
/// Form integration
/// </summary>
public class FormIntegration
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string DataMapping { get; set; } = string.Empty;
    public FormCondition? Condition { get; set; }
    public bool IsAsync { get; set; } = true;
    public int RetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Form submission
/// </summary>
public class FormSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FormId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public FormSubmissionStatus Status { get; set; } = FormSubmissionStatus.Submitted;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<FormSubmissionFile> Files { get; set; } = new();
    public FormSubmissionMetadata Metadata { get; set; } = new();
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingResult { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> ProcessingData { get; set; } = new();
}

/// <summary>
/// Form submission status
/// </summary>
public enum FormSubmissionStatus
{
    Draft,
    Submitted,
    Processing,
    Completed,
    Failed,
    Rejected,
    Archived
}

/// <summary>
/// Form submission file
/// </summary>
public class FormSubmissionFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FieldName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; } = false;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Form submission metadata
/// </summary>
public class FormSubmissionMetadata
{
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Referrer { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public TimeSpan CompletionTime { get; set; }
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Form template
/// </summary>
public class FormTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public FormDefinition Definition { get; set; } = new();
    public string PreviewUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public List<FormTemplateParameter> Parameters { get; set; } = new();
    public int UsageCount { get; set; }
    public double Rating { get; set; }
    public bool IsOfficial { get; set; } = false;
    public bool IsFree { get; set; } = true;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Form template parameter
/// </summary>
public class FormTemplateParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
    public bool IsRequired { get; set; } = false;
    public string Description { get; set; } = string.Empty;
    public List<object> AllowedValues { get; set; } = new();
}

/// <summary>
/// Form preview
/// </summary>
public class FormPreview
{
    public string FormId { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string Css { get; set; } = string.Empty;
    public string JavaScript { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan? ExpiresIn { get; set; }
}

/// <summary>
/// Preview options
/// </summary>
public class PreviewOptions
{
    public PreviewMode Mode { get; set; } = PreviewMode.Desktop;
    public bool IncludeTestData { get; set; } = false;
    public string Theme { get; set; } = string.Empty;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Preview modes
/// </summary>
public enum PreviewMode
{
    Desktop,
    Tablet,
    Mobile,
    Print,
    Email
}

/// <summary>
/// Form validation result
/// </summary>
public class FormValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Form submission result
/// </summary>
public class FormSubmissionResult
{
    public bool IsSuccess { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public string ConfirmationMessage { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Form code result
/// </summary>
public class FormCodeResult
{
    public bool IsSuccess { get; set; }
    public string Code { get; set; } = string.Empty;
    public CodeGenerationLanguage Language { get; set; }
    public string FileName { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Code generation options
/// </summary>
public class CodeGenerationOptions
{
    public CodeGenerationLanguage Language { get; set; } = CodeGenerationLanguage.CSharp;
    public CodeGenerationFramework Framework { get; set; } = CodeGenerationFramework.Blazor;
    public bool IncludeValidation { get; set; } = true;
    public bool IncludeStylesheet { get; set; } = true;
    public bool IncludeJavaScript { get; set; } = true;
    public bool GenerateDTO { get; set; } = true;
    public string Namespace { get; set; } = "BizCore.Forms";
    public string ClassName { get; set; } = string.Empty;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

/// <summary>
/// Code generation languages
/// </summary>
public enum CodeGenerationLanguage
{
    CSharp,
    TypeScript,
    JavaScript,
    HTML,
    CSS,
    Vue,
    React,
    Angular
}

/// <summary>
/// Code generation frameworks
/// </summary>
public enum CodeGenerationFramework
{
    Blazor,
    MVC,
    WebAPI,
    React,
    Vue,
    Angular,
    Plain
}

/// <summary>
/// Form result wrapper
/// </summary>
public class FormResult
{
    public bool IsSuccess { get; set; }
    public FormDefinition? Form { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static FormResult Success(FormDefinition form) =>
        new() { IsSuccess = true, Form = form };

    public static FormResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static FormResult ValidationFailure(List<ValidationError> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}

/// <summary>
/// Create form request
/// </summary>
public class CreateFormRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;
    public bool IsTemplate { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Create form from template request
/// </summary>
public class CreateFormFromTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Form query parameters
/// </summary>
public class FormQuery
{
    public string? TenantId { get; set; }
    public FormStatus? Status { get; set; }
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool? IsTemplate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string SortBy { get; set; } = "UpdatedAt";
    public bool SortDescending { get; set; } = true;
}