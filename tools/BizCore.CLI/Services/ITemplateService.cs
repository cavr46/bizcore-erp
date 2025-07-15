using BizCore.CLI.Models;

namespace BizCore.CLI.Services;

/// <summary>
/// Template service for managing code templates
/// </summary>
public interface ITemplateService
{
    Task<string> GetTemplateAsync(string templateName);
    Task<IEnumerable<ProjectTemplate>> ListTemplatesAsync();
    Task InstallTemplateAsync(string source, string name);
    Task<string> RenderTemplateAsync(string templateContent, Dictionary<string, object> parameters);
}

/// <summary>
/// Template service implementation
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly Dictionary<string, string> _builtInTemplates;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
        _builtInTemplates = InitializeBuiltInTemplates();
    }

    public async Task<string> GetTemplateAsync(string templateName)
    {
        if (_builtInTemplates.ContainsKey(templateName))
        {
            return _builtInTemplates[templateName];
        }

        // Try to load from file system
        var templatePath = Path.Combine("Templates", $"{templateName}.template");
        if (File.Exists(templatePath))
        {
            return await File.ReadAllTextAsync(templatePath);
        }

        throw new FileNotFoundException($"Template '{templateName}' not found");
    }

    public async Task<IEnumerable<ProjectTemplate>> ListTemplatesAsync()
    {
        var templates = new List<ProjectTemplate>();
        
        // Add built-in templates
        templates.AddRange(GetBuiltInTemplates());
        
        // Add custom templates from file system
        var customTemplates = await GetCustomTemplatesAsync();
        templates.AddRange(customTemplates);

        return templates;
    }

    public async Task InstallTemplateAsync(string source, string name)
    {
        try
        {
            CliHelpers.WriteInfo($"Installing template: {name} from {source}");
            
            // Implementation depends on source type
            switch (source.ToLower())
            {
                case "git":
                    await InstallFromGitAsync(name);
                    break;
                case "local":
                    await InstallFromLocalAsync(name);
                    break;
                case "marketplace":
                    await InstallFromMarketplaceAsync(name);
                    break;
                default:
                    throw new NotSupportedException($"Template source '{source}' is not supported");
            }

            CliHelpers.WriteSuccess($"Template '{name}' installed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install template {TemplateName}", name);
            CliHelpers.WriteError($"Failed to install template: {ex.Message}");
        }
    }

    public async Task<string> RenderTemplateAsync(string templateContent, Dictionary<string, object> parameters)
    {
        var result = templateContent;
        
        foreach (var parameter in parameters)
        {
            var placeholder = $"{{{{{parameter.Key}}}}}";
            result = result.Replace(placeholder, parameter.Value?.ToString() ?? string.Empty);
        }

        return result;
    }

    private Dictionary<string, string> InitializeBuiltInTemplates()
    {
        return new Dictionary<string, string>
        {
            ["GrainInterface"] = GetGrainInterfaceTemplate(),
            ["Grain"] = GetGrainTemplate(),
            ["GrainState"] = GetGrainStateTemplate(),
            ["GrainEvents"] = GetGrainEventsTemplate(),
            ["GrainTest"] = GetGrainTestTemplate(),
            ["ApiController"] = GetApiControllerTemplate(),
            ["BlazorListComponent"] = GetBlazorListComponentTemplate(),
            ["BlazorDetailComponent"] = GetBlazorDetailComponentTemplate(),
            ["BlazorComponent_Form"] = GetBlazorFormComponentTemplate(),
            ["BlazorComponent_List"] = GetBlazorListComponentTemplate(),
            ["BlazorComponent_Detail"] = GetBlazorDetailComponentTemplate(),
            ["BlazorComponentCodeBehind_Form"] = GetBlazorFormCodeBehindTemplate(),
            ["BlazorComponentCodeBehind_List"] = GetBlazorListCodeBehindTemplate(),
            ["BlazorComponentCodeBehind_Detail"] = GetBlazorDetailCodeBehindTemplate(),
            ["Entity"] = GetEntityTemplate(),
            ["EntityDTO"] = GetEntityDTOTemplate(),
            ["EntityApiController"] = GetEntityApiControllerTemplate(),
            ["AggregateRoot"] = GetAggregateRootTemplate(),
            ["ValueObject"] = GetValueObjectTemplate(),
            ["UnitTest"] = GetUnitTestTemplate(),
            ["IntegrationTest"] = GetIntegrationTestTemplate(),
            ["IntegrationService"] = GetIntegrationServiceTemplate(),
            ["IntegrationConfiguration"] = GetIntegrationConfigurationTemplate(),
            ["WorkflowDefinition"] = GetWorkflowDefinitionTemplate(),
            ["WorkflowActivity"] = GetWorkflowActivityTemplate(),
            ["WorkflowTest"] = GetWorkflowTestTemplate(),
            ["ReportClass"] = GetReportClassTemplate(),
            ["ReportQuery"] = GetReportQueryTemplate(),
            ["ReportComponent"] = GetReportComponentTemplate()
        };
    }

    private IEnumerable<ProjectTemplate> GetBuiltInTemplates()
    {
        return new[]
        {
            new ProjectTemplate
            {
                Id = "basic",
                Name = "Basic ERP",
                Description = "Basic ERP with core modules",
                Category = "Starter",
                Features = new[] { "Accounting", "Inventory", "Sales" },
                IsDefault = true
            },
            new ProjectTemplate
            {
                Id = "retail",
                Name = "Retail Management",
                Description = "Complete retail management solution",
                Category = "Industry",
                Features = new[] { "POS", "Inventory", "Customer Management" }
            },
            new ProjectTemplate
            {
                Id = "manufacturing",
                Name = "Manufacturing ERP",
                Description = "Manufacturing ERP with MRP",
                Category = "Industry",
                Features = new[] { "MRP", "Production", "Quality Control" }
            }
        };
    }

    private async Task<IEnumerable<ProjectTemplate>> GetCustomTemplatesAsync()
    {
        var templates = new List<ProjectTemplate>();
        
        // Load custom templates from file system
        var templatesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BizCore", "Templates");
        
        if (Directory.Exists(templatesPath))
        {
            var templateFiles = Directory.GetFiles(templatesPath, "*.json");
            
            foreach (var templateFile in templateFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(templateFile);
                    var template = JsonSerializer.Deserialize<ProjectTemplate>(json);
                    if (template != null)
                    {
                        templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load template from {TemplateFile}", templateFile);
                }
            }
        }

        return templates;
    }

    private async Task InstallFromGitAsync(string name)
    {
        // Implementation for installing template from Git repository
        throw new NotImplementedException("Git template installation not yet implemented");
    }

    private async Task InstallFromLocalAsync(string name)
    {
        // Implementation for installing template from local path
        throw new NotImplementedException("Local template installation not yet implemented");
    }

    private async Task InstallFromMarketplaceAsync(string name)
    {
        // Implementation for installing template from marketplace
        throw new NotImplementedException("Marketplace template installation not yet implemented");
    }

    #region Template Definitions

    private string GetGrainInterfaceTemplate()
    {
        return @"using Orleans;
using BizCore.Orleans.Contracts.Base;

namespace {{Namespace}};

/// <summary>
/// {{GrainName}} grain interface
/// </summary>
public interface I{{GrainName}}Grain : IGrainWithGuidKey
{
    Task<Result<Guid>> CreateAsync(Create{{GrainName}}Command command);
    Task<Result> UpdateAsync(Update{{GrainName}}Command command);
    Task<Result> DeleteAsync();
    Task<Result<{{GrainName}}State>> GetAsync();
    Task<Result<IEnumerable<{{GrainName}}State>>> GetAllAsync();
}

/// <summary>
/// {{GrainName}} commands
/// </summary>
[GenerateSerializer]
public record Create{{GrainName}}Command(
    Guid TenantId,
    string Name,
    string Description
);

[GenerateSerializer]
public record Update{{GrainName}}Command(
    string Name,
    string Description
);

/// <summary>
/// {{GrainName}} state
/// </summary>
[GenerateSerializer]
public class {{GrainName}}State
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}";
    }

    private string GetGrainTemplate()
    {
        return @"using Orleans;
using Orleans.Runtime;
using BizCore.Orleans.Core.Base;

namespace {{Namespace}};

/// <summary>
/// {{GrainName}} grain implementation
/// </summary>
public class {{GrainName}}Grain : TenantGrainBase<{{GrainName}}State>, I{{GrainName}}Grain
{
    private readonly ILogger<{{GrainName}}Grain> _logger;

    public {{GrainName}}Grain(ILogger<{{GrainName}}Grain> logger)
    {
        _logger = logger;
    }

    public async Task<Result<Guid>> CreateAsync(Create{{GrainName}}Command command)
    {
        try
        {
            if (State.Id != Guid.Empty)
            {
                return Result<Guid>.Failure(""{{GrainName}} already exists"");
            }

            State.Id = this.GetPrimaryKey();
            State.TenantId = command.TenantId;
            State.Name = command.Name;
            State.Description = command.Description;
            State.CreatedAt = DateTime.UtcNow;
            State.UpdatedAt = DateTime.UtcNow;
            State.IsActive = true;

            await WriteStateAsync();

            _logger.LogInformation(""{{GrainName}} {Id} created successfully"", State.Id);
            return Result<Guid>.Success(State.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to create {{GrainName}}"");
            return Result<Guid>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateAsync(Update{{GrainName}}Command command)
    {
        try
        {
            if (State.Id == Guid.Empty)
            {
                return Result.Failure(""{{GrainName}} not found"");
            }

            State.Name = command.Name;
            State.Description = command.Description;
            State.UpdatedAt = DateTime.UtcNow;

            await WriteStateAsync();

            _logger.LogInformation(""{{GrainName}} {Id} updated successfully"", State.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to update {{GrainName}} {Id}"", State.Id);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync()
    {
        try
        {
            if (State.Id == Guid.Empty)
            {
                return Result.Failure(""{{GrainName}} not found"");
            }

            State.IsActive = false;
            State.UpdatedAt = DateTime.UtcNow;

            await WriteStateAsync();

            _logger.LogInformation(""{{GrainName}} {Id} deleted successfully"", State.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to delete {{GrainName}} {Id}"", State.Id);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<{{GrainName}}State>> GetAsync()
    {
        try
        {
            if (State.Id == Guid.Empty)
            {
                return Result<{{GrainName}}State>.Failure(""{{GrainName}} not found"");
            }

            return Result<{{GrainName}}State>.Success(State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to get {{GrainName}} {Id}"", State.Id);
            return Result<{{GrainName}}State>.Failure(ex.Message);
        }
    }

    public async Task<Result<IEnumerable<{{GrainName}}State>>> GetAllAsync()
    {
        try
        {
            // Implementation depends on your data access strategy
            // This is a placeholder
            var results = new List<{{GrainName}}State> { State };
            return Result<IEnumerable<{{GrainName}}State>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to get all {{GrainName}}s"");
            return Result<IEnumerable<{{GrainName}}State>>.Failure(ex.Message);
        }
    }
}";
    }

    private string GetGrainStateTemplate()
    {
        return @"namespace {{Namespace}};

/// <summary>
/// {{GrainName}} grain state
/// </summary>
[GenerateSerializer]
public class {{GrainName}}State
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Properties { get; set; } = new();
}";
    }

    private string GetGrainEventsTemplate()
    {
        return @"namespace {{Namespace}};

/// <summary>
/// {{GrainName}} domain events
/// </summary>
[GenerateSerializer]
public abstract record {{GrainName}}Event(Guid Id, Guid TenantId, DateTime OccurredAt);

[GenerateSerializer]
public record {{GrainName}}Created(Guid Id, Guid TenantId, DateTime OccurredAt, string Name) : {{GrainName}}Event(Id, TenantId, OccurredAt);

[GenerateSerializer]
public record {{GrainName}}Updated(Guid Id, Guid TenantId, DateTime OccurredAt, string Name) : {{GrainName}}Event(Id, TenantId, OccurredAt);

[GenerateSerializer]
public record {{GrainName}}Deleted(Guid Id, Guid TenantId, DateTime OccurredAt) : {{GrainName}}Event(Id, TenantId, OccurredAt);";
    }

    private string GetGrainTestTemplate()
    {
        return @"using Orleans.TestingHost;
using Xunit;

namespace {{Namespace}};

/// <summary>
/// {{GrainName}} grain tests
/// </summary>
public class {{GrainName}}GrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;

    public {{GrainName}}GrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
    }

    [Fact]
    public async Task Create{{GrainName}}_ShouldSucceed()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<I{{GrainName}}Grain>(Guid.NewGuid());
        var command = new Create{{GrainName}}Command(
            Guid.NewGuid(),
            ""Test {{GrainName}}"",
            ""Test Description""
        );

        // Act
        var result = await grain.CreateAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Update{{GrainName}}_ShouldSucceed()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<I{{GrainName}}Grain>(Guid.NewGuid());
        var createCommand = new Create{{GrainName}}Command(
            Guid.NewGuid(),
            ""Test {{GrainName}}"",
            ""Test Description""
        );
        await grain.CreateAsync(createCommand);

        var updateCommand = new Update{{GrainName}}Command(
            ""Updated {{GrainName}}"",
            ""Updated Description""
        );

        // Act
        var result = await grain.UpdateAsync(updateCommand);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Get{{GrainName}}_ShouldReturnState()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<I{{GrainName}}Grain>(Guid.NewGuid());
        var command = new Create{{GrainName}}Command(
            Guid.NewGuid(),
            ""Test {{GrainName}}"",
            ""Test Description""
        );
        await grain.CreateAsync(command);

        // Act
        var result = await grain.GetAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(""Test {{GrainName}}"", result.Value.Name);
    }
}";
    }

    private string GetApiControllerTemplate()
    {
        return @"using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace {{Namespace}};

/// <summary>
/// {{ControllerName}} API controller
/// </summary>
[ApiController]
[Route(""api/[controller]"")]
public class {{ControllerName}}Controller : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<{{ControllerName}}Controller> _logger;

    public {{ControllerName}}Controller(IGrainFactory grainFactory, ILogger<{{ControllerName}}Controller> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    [HttpGet(""{id}"")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var grain = _grainFactory.GetGrain<I{{EntityName}}Grain>(id);
            var result = await grain.GetAsync();
            
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            
            return NotFound(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to get {{EntityName}} {Id}"", id);
            return StatusCode(500, ""Internal server error"");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Create{{EntityName}}Command command)
    {
        try
        {
            var grain = _grainFactory.GetGrain<I{{EntityName}}Grain>(Guid.NewGuid());
            var result = await grain.CreateAsync(command);
            
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(Get), new { id = result.Value }, result.Value);
            }
            
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to create {{EntityName}}"");
            return StatusCode(500, ""Internal server error"");
        }
    }

    [HttpPut(""{id}"")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Update{{EntityName}}Command command)
    {
        try
        {
            var grain = _grainFactory.GetGrain<I{{EntityName}}Grain>(id);
            var result = await grain.UpdateAsync(command);
            
            if (result.IsSuccess)
            {
                return NoContent();
            }
            
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to update {{EntityName}} {Id}"", id);
            return StatusCode(500, ""Internal server error"");
        }
    }

    [HttpDelete(""{id}"")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var grain = _grainFactory.GetGrain<I{{EntityName}}Grain>(id);
            var result = await grain.DeleteAsync();
            
            if (result.IsSuccess)
            {
                return NoContent();
            }
            
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to delete {{EntityName}} {Id}"", id);
            return StatusCode(500, ""Internal server error"");
        }
    }
}";
    }

    private string GetBlazorListComponentTemplate()
    {
        return @"@page ""/{{ModuleName}}/{{EntityName}}"" 
@using MudBlazor
@inject IGrainFactory GrainFactory

<PageTitle>{{EntityName}} List</PageTitle>

<MudContainer MaxWidth=""MaxWidth.ExtraLarge"">
    <MudText Typo=""Typo.h4"" Class=""mb-4"">{{EntityName}} Management</MudText>
    
    <MudPaper Class=""pa-4"">
        <MudButton Variant=""Variant.Filled"" 
                   Color=""Color.Primary"" 
                   StartIcon=""@Icons.Material.Filled.Add""
                   OnClick=""@(() => NavigationManager.NavigateTo($""/{{ModuleName}}/{{EntityName}}/create""))"">
            Create {{EntityName}}
        </MudButton>
        
        <MudTable Items=""@_items"" 
                  Dense=""@true"" 
                  Hover=""@true"" 
                  Loading=""@_loading""
                  Class=""mt-4"">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Description</MudTh>
                <MudTh>Created</MudTh>
                <MudTh>Actions</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd DataLabel=""Name"">@context.Name</MudTd>
                <MudTd DataLabel=""Description"">@context.Description</MudTd>
                <MudTd DataLabel=""Created"">@context.CreatedAt.ToString(""MM/dd/yyyy"")</MudTd>
                <MudTd DataLabel=""Actions"">
                    <MudButton Size=""Size.Small"" 
                               Variant=""Variant.Outlined"" 
                               Color=""Color.Primary""
                               OnClick=""@(() => Edit(context.Id))"">
                        Edit
                    </MudButton>
                    <MudButton Size=""Size.Small"" 
                               Variant=""Variant.Outlined"" 
                               Color=""Color.Error""
                               OnClick=""@(() => Delete(context.Id))"">
                        Delete
                    </MudButton>
                </MudTd>
            </RowTemplate>
        </MudTable>
    </MudPaper>
</MudContainer>";
    }

    private string GetBlazorDetailComponentTemplate()
    {
        return @"@page ""/{{ModuleName}}/{{EntityName}}/{Id:guid}""
@page ""/{{ModuleName}}/{{EntityName}}/create""
@using MudBlazor
@inject IGrainFactory GrainFactory
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar

<PageTitle>{{EntityName}} Details</PageTitle>

<MudContainer MaxWidth=""MaxWidth.Medium"">
    <MudText Typo=""Typo.h4"" Class=""mb-4"">
        @(_isEdit ? $""Edit {{EntityName}}"" : ""Create {{EntityName}}"")
    </MudText>
    
    <MudPaper Class=""pa-4"">
        <MudForm @ref=""_form"" Model=""@_model"">
            <MudTextField @bind-Value=""_model.Name""
                          Label=""Name""
                          Required=""true""
                          RequiredError=""Name is required""
                          Class=""mb-3"" />
            
            <MudTextField @bind-Value=""_model.Description""
                          Label=""Description""
                          Lines=""3""
                          Class=""mb-3"" />
            
            <MudButton Variant=""Variant.Filled""
                       Color=""Color.Primary""
                       StartIcon=""@Icons.Material.Filled.Save""
                       OnClick=""@Save""
                       Class=""mr-2"">
                Save
            </MudButton>
            
            <MudButton Variant=""Variant.Outlined""
                       Color=""Color.Secondary""
                       StartIcon=""@Icons.Material.Filled.Cancel""
                       OnClick=""@Cancel"">
                Cancel
            </MudButton>
        </MudForm>
    </MudPaper>
</MudContainer>";
    }

    private string GetBlazorFormComponentTemplate()
    {
        return GetBlazorDetailComponentTemplate();
    }

    private string GetBlazorFormCodeBehindTemplate()
    {
        return @"using MudBlazor;
using Microsoft.AspNetCore.Components;
using Orleans;

namespace {{Namespace}};

public partial class {{ComponentName}}
{
    [Parameter] public Guid? Id { get; set; }
    [Inject] private IGrainFactory GrainFactory { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private MudForm _form = null!;
    private {{ComponentName}}Model _model = new();
    private bool _isEdit => Id.HasValue;

    protected override async Task OnInitializedAsync()
    {
        if (_isEdit)
        {
            await LoadData();
        }
    }

    private async Task LoadData()
    {
        try
        {
            var grain = GrainFactory.GetGrain<I{{EntityName}}Grain>(Id!.Value);
            var result = await grain.GetAsync();
            
            if (result.IsSuccess)
            {
                _model = new {{ComponentName}}Model
                {
                    Name = result.Value.Name,
                    Description = result.Value.Description
                };
            }
            else
            {
                Snackbar.Add($""Failed to load {{EntityName}}: {result.Error}"", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($""Error loading {{EntityName}}: {ex.Message}"", Severity.Error);
        }
    }

    private async Task Save()
    {
        try
        {
            await _form.Validate();
            if (!_form.IsValid) return;

            if (_isEdit)
            {
                var grain = GrainFactory.GetGrain<I{{EntityName}}Grain>(Id!.Value);
                var command = new Update{{EntityName}}Command(_model.Name, _model.Description);
                var result = await grain.UpdateAsync(command);
                
                if (result.IsSuccess)
                {
                    Snackbar.Add(""{{EntityName}} updated successfully"", Severity.Success);
                    NavigationManager.NavigateTo(""/{{ModuleName}}/{{EntityName}}"");
                }
                else
                {
                    Snackbar.Add($""Failed to update {{EntityName}}: {result.Error}"", Severity.Error);
                }
            }
            else
            {
                var grain = GrainFactory.GetGrain<I{{EntityName}}Grain>(Guid.NewGuid());
                var command = new Create{{EntityName}}Command(Guid.NewGuid(), _model.Name, _model.Description);
                var result = await grain.CreateAsync(command);
                
                if (result.IsSuccess)
                {
                    Snackbar.Add(""{{EntityName}} created successfully"", Severity.Success);
                    NavigationManager.NavigateTo(""/{{ModuleName}}/{{EntityName}}"");
                }
                else
                {
                    Snackbar.Add($""Failed to create {{EntityName}}: {result.Error}"", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($""Error saving {{EntityName}}: {ex.Message}"", Severity.Error);
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo(""/{{ModuleName}}/{{EntityName}}"");
    }

    private class {{ComponentName}}Model
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}";
    }

    private string GetBlazorListCodeBehindTemplate()
    {
        return @"using MudBlazor;
using Microsoft.AspNetCore.Components;
using Orleans;

namespace {{Namespace}};

public partial class {{ComponentName}}
{
    [Inject] private IGrainFactory GrainFactory { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<{{EntityName}}State> _items = new();
    private bool _loading = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            _loading = true;
            StateHasChanged();

            // Implementation depends on your data access strategy
            // This is a placeholder for getting all items
            _items = new List<{{EntityName}}State>();
        }
        catch (Exception ex)
        {
            Snackbar.Add($""Error loading data: {ex.Message}"", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private void Edit(Guid id)
    {
        NavigationManager.NavigateTo($""/{{ModuleName}}/{{EntityName}}/{id}"");
    }

    private async Task Delete(Guid id)
    {
        try
        {
            var parameters = new DialogParameters();
            parameters.Add(""ContentText"", ""Are you sure you want to delete this {{EntityName}}?"");
            parameters.Add(""ButtonText"", ""Delete"");
            parameters.Add(""Color"", Color.Error);

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
            var dialog = await DialogService.ShowAsync<ConfirmationDialog>(""Delete {{EntityName}}"", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var grain = GrainFactory.GetGrain<I{{EntityName}}Grain>(id);
                var deleteResult = await grain.DeleteAsync();
                
                if (deleteResult.IsSuccess)
                {
                    Snackbar.Add(""{{EntityName}} deleted successfully"", Severity.Success);
                    await LoadData();
                }
                else
                {
                    Snackbar.Add($""Failed to delete {{EntityName}}: {deleteResult.Error}"", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($""Error deleting {{EntityName}}: {ex.Message}"", Severity.Error);
        }
    }
}";
    }

    private string GetBlazorDetailCodeBehindTemplate()
    {
        return GetBlazorFormCodeBehindTemplate();
    }

    private string GetEntityTemplate()
    {
        return @"using BizCore.Domain.Common;

namespace {{Namespace}};

/// <summary>
/// {{EntityName}} entity
/// </summary>
public class {{EntityName}} : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<{{EntityName}}Item> Items { get; set; } = new List<{{EntityName}}Item>();
    
    // Domain methods
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}";
    }

    private string GetEntityDTOTemplate()
    {
        return @"namespace {{Namespace}};

/// <summary>
/// {{EntityName}} data transfer objects
/// </summary>
public record {{EntityName}}Dto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record Create{{EntityName}}Dto(
    string Name,
    string Description
);

public record Update{{EntityName}}Dto(
    string Name,
    string Description
);";
    }

    private string GetEntityApiControllerTemplate()
    {
        return GetApiControllerTemplate();
    }

    private string GetAggregateRootTemplate()
    {
        return @"using BizCore.Domain.Common;

namespace {{Namespace}};

/// <summary>
/// {{EntityName}} aggregate root
/// </summary>
public class {{EntityName}} : AggregateRoot
{
    private readonly List<{{EntityName}}Item> _items = new();
    
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    
    public IReadOnlyList<{{EntityName}}Item> Items => _items.AsReadOnly();
    
    // Factory method
    public static {{EntityName}} Create(string name, string description)
    {
        var entity = new {{EntityName}}();
        entity.SetName(name);
        entity.SetDescription(description);
        entity.AddDomainEvent(new {{EntityName}}CreatedEvent(entity.Id, entity.Name));
        return entity;
    }
    
    // Domain methods
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(""Name cannot be empty"", nameof(name));
            
        Name = name;
        AddDomainEvent(new {{EntityName}}NameChangedEvent(Id, name));
    }
    
    public void SetDescription(string description)
    {
        Description = description ?? string.Empty;
    }
    
    public void Activate()
    {
        if (IsActive) return;
        
        IsActive = true;
        AddDomainEvent(new {{EntityName}}ActivatedEvent(Id));
    }
    
    public void Deactivate()
    {
        if (!IsActive) return;
        
        IsActive = false;
        AddDomainEvent(new {{EntityName}}DeactivatedEvent(Id));
    }
}

// Domain events
public record {{EntityName}}CreatedEvent(Guid Id, string Name) : IDomainEvent;
public record {{EntityName}}NameChangedEvent(Guid Id, string Name) : IDomainEvent;
public record {{EntityName}}ActivatedEvent(Guid Id) : IDomainEvent;
public record {{EntityName}}DeactivatedEvent(Guid Id) : IDomainEvent;";
    }

    private string GetValueObjectTemplate()
    {
        return @"using BizCore.Domain.Common;

namespace {{Namespace}};

/// <summary>
/// {{EntityName}} identifier value object
/// </summary>
public record {{EntityName}}Id(Guid Value) : IValueObject
{
    public static {{EntityName}}Id New() => new(Guid.NewGuid());
    public static {{EntityName}}Id From(Guid value) => new(value);
    
    public static implicit operator Guid({{EntityName}}Id id) => id.Value;
    public static implicit operator {{EntityName}}Id(Guid value) => new(value);
    
    public override string ToString() => Value.ToString();
}";
    }

    private string GetUnitTestTemplate()
    {
        return @"using Xunit;

namespace {{Namespace}};

/// <summary>
/// {{EntityName}} unit tests
/// </summary>
public class {{EntityName}}Tests
{
    [Fact]
    public void Create{{EntityName}}_ShouldSucceed()
    {
        // Arrange
        var name = ""Test {{EntityName}}"";
        var description = ""Test Description"";
        
        // Act
        var entity = {{EntityName}}.Create(name, description);
        
        // Assert
        Assert.NotNull(entity);
        Assert.Equal(name, entity.Name);
        Assert.Equal(description, entity.Description);
        Assert.True(entity.IsActive);
    }
    
    [Fact]
    public void SetName_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var entity = {{EntityName}}.Create(""Test"", ""Description"");
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => entity.SetName(string.Empty));
    }
    
    [Fact]
    public void Activate_WhenInactive_ShouldActivate()
    {
        // Arrange
        var entity = {{EntityName}}.Create(""Test"", ""Description"");
        entity.Deactivate();
        
        // Act
        entity.Activate();
        
        // Assert
        Assert.True(entity.IsActive);
    }
    
    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivate()
    {
        // Arrange
        var entity = {{EntityName}}.Create(""Test"", ""Description"");
        
        // Act
        entity.Deactivate();
        
        // Assert
        Assert.False(entity.IsActive);
    }
}";
    }

    private string GetIntegrationTestTemplate()
    {
        return @"using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Xunit;

namespace {{Namespace}};

/// <summary>
/// {{EntityName}} integration tests
/// </summary>
public class {{EntityName}}IntegrationTests : IClassFixture<TestClusterFixture>
{
    private readonly TestCluster _cluster;
    private readonly IServiceProvider _serviceProvider;

    public {{EntityName}}IntegrationTests(TestClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
        _serviceProvider = _cluster.ServiceProvider;
    }

    [Fact]
    public async Task Create{{EntityName}}_EndToEnd_ShouldSucceed()
    {
        // Arrange
        var grainFactory = _serviceProvider.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<I{{EntityName}}Grain>(Guid.NewGuid());
        
        var command = new Create{{EntityName}}Command(
            Guid.NewGuid(),
            ""Integration Test {{EntityName}}"",
            ""Integration Test Description""
        );

        // Act
        var createResult = await grain.CreateAsync(command);
        var getResult = await grain.GetAsync();

        // Assert
        Assert.True(createResult.IsSuccess);
        Assert.True(getResult.IsSuccess);
        Assert.Equal(command.Name, getResult.Value.Name);
        Assert.Equal(command.Description, getResult.Value.Description);
    }

    [Fact]
    public async Task Update{{EntityName}}_EndToEnd_ShouldSucceed()
    {
        // Arrange
        var grainFactory = _serviceProvider.GetRequiredService<IGrainFactory>();
        var grain = grainFactory.GetGrain<I{{EntityName}}Grain>(Guid.NewGuid());
        
        var createCommand = new Create{{EntityName}}Command(
            Guid.NewGuid(),
            ""Original Name"",
            ""Original Description""
        );
        await grain.CreateAsync(createCommand);

        var updateCommand = new Update{{EntityName}}Command(
            ""Updated Name"",
            ""Updated Description""
        );

        // Act
        var updateResult = await grain.UpdateAsync(updateCommand);
        var getResult = await grain.GetAsync();

        // Assert
        Assert.True(updateResult.IsSuccess);
        Assert.True(getResult.IsSuccess);
        Assert.Equal(updateCommand.Name, getResult.Value.Name);
        Assert.Equal(updateCommand.Description, getResult.Value.Description);
    }
}";
    }

    private string GetIntegrationServiceTemplate()
    {
        return @"using Microsoft.Extensions.Options;

namespace {{Namespace}};

/// <summary>
/// {{Provider}} integration service
/// </summary>
public interface I{{Provider}}Service
{
    Task<{{Provider}}Result> ProcessAsync({{Provider}}Request request);
    Task<bool> TestConnectionAsync();
}

/// <summary>
/// {{Provider}} service implementation
/// </summary>
public class {{Provider}}Service : I{{Provider}}Service
{
    private readonly {{Provider}}Configuration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<{{Provider}}Service> _logger;

    public {{Provider}}Service(
        IOptions<{{Provider}}Configuration> configuration,
        HttpClient httpClient,
        ILogger<{{Provider}}Service> logger)
    {
        _configuration = configuration.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<{{Provider}}Result> ProcessAsync({{Provider}}Request request)
    {
        try
        {
            _logger.LogInformation(""Processing {{Provider}} request"");

            // Implementation specific to {{Provider}}
            // This is a placeholder
            
            return new {{Provider}}Result
            {
                Success = true,
                Message = ""{{Provider}} request processed successfully""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Failed to process {{Provider}} request"");
            return new {{Provider}}Result
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Test connection to {{Provider}}
            var response = await _httpClient.GetAsync(_configuration.BaseUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""{{Provider}} connection test failed"");
            return false;
        }
    }
}

/// <summary>
/// {{Provider}} request model
/// </summary>
public class {{Provider}}Request
{
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// {{Provider}} result model
/// </summary>
public class {{Provider}}Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object Data { get; set; }
}";
    }

    private string GetIntegrationConfigurationTemplate()
    {
        return @"namespace {{Namespace}};

/// <summary>
/// {{Provider}} configuration
/// </summary>
public class {{Provider}}Configuration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public Dictionary<string, string> Headers { get; set; } = new();
}";
    }

    private string GetWorkflowDefinitionTemplate()
    {
        return @"using Elsa.Activities;
using Elsa.Expressions;
using Elsa.Services;

namespace {{Namespace}};

/// <summary>
/// {{WorkflowName}} workflow definition
/// {{Description}}
/// </summary>
public class {{WorkflowName}}Workflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .StartWith<WriteLine>(x => x.Text = ""Starting {{WorkflowName}} workflow"")
            .Then<WriteLine>(x => x.Text = ""{{WorkflowName}} workflow completed"");
    }
}";
    }

    private string GetWorkflowActivityTemplate()
    {
        return @"using Elsa.Activities;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;

namespace {{Namespace}};

/// <summary>
/// {{ActivityName}} workflow activity
/// </summary>
[Activity(
    Category = ""{{WorkflowName}}"",
    Description = ""{{ActivityName}} activity"",
    Outcomes = new[] { OutcomeNames.Done }
)]
public class {{ActivityName}}Activity : Activity
{
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
    {
        // Activity implementation
        await Task.CompletedTask;
        
        return Done();
    }
}";
    }

    private string GetWorkflowTestTemplate()
    {
        return @"using Elsa.Services;
using Elsa.Testing;
using Xunit;

namespace {{Namespace}};

/// <summary>
/// {{WorkflowName}} workflow tests
/// </summary>
public class {{WorkflowName}}WorkflowTests : WorkflowTest
{
    [Fact]
    public async Task {{WorkflowName}}Workflow_ShouldComplete()
    {
        // Arrange
        var workflow = new {{WorkflowName}}Workflow();
        
        // Act
        var result = await RunWorkflowAsync(workflow);
        
        // Assert
        Assert.True(result.IsFinished);
        Assert.True(result.IsCompleted);
    }
}";
    }

    private string GetReportClassTemplate()
    {
        return @"using BizCore.Reporting.Core;

namespace {{Namespace}};

/// <summary>
/// {{ReportName}} report
/// </summary>
public class {{ReportName}}Report : IReport
{
    public string Name => ""{{ReportName}}"";
    public string Description => ""{{ReportName}} report"";
    public string Category => ""{{DataSource}}"";
    
    public async Task<ReportResult> GenerateAsync(ReportParameters parameters)
    {
        try
        {
            // Implementation for {{ReportName}} report
            var data = await GetReportDataAsync(parameters);
            
            return new ReportResult
            {
                Success = true,
                Data = data,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ReportResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
    
    private async Task<object> GetReportDataAsync(ReportParameters parameters)
    {
        // Query data from {{DataSource}}
        // This is a placeholder
        return new { Message = ""{{ReportName}} data"" };
    }
}";
    }

    private string GetReportQueryTemplate()
    {
        return @"using BizCore.Reporting.Core;

namespace {{Namespace}};

/// <summary>
/// {{ReportName}} query
/// </summary>
public class {{ReportName}}Query : IReportQuery
{
    public string Name => ""{{ReportName}}"";
    public string DataSource => ""{{DataSource}}"";
    
    public async Task<QueryResult> ExecuteAsync(QueryParameters parameters)
    {
        try
        {
            // SQL query for {{ReportName}}
            var sql = @""
                SELECT {{Fields}}
                FROM {{DataSource}}
                WHERE CreatedAt >= @StartDate
                AND CreatedAt <= @EndDate
                ORDER BY CreatedAt DESC
            "";
            
            // Execute query
            // This is a placeholder
            var data = new List<object>();
            
            return new QueryResult
            {
                Success = true,
                Data = data,
                TotalCount = data.Count
            };
        }
        catch (Exception ex)
        {
            return new QueryResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}";
    }

    private string GetReportComponentTemplate()
    {
        return @"@page ""/reports/{{ReportName}}""
@using MudBlazor
@using BizCore.Reporting.Core
@inject IReportService ReportService

<PageTitle>{{ReportName}} Report</PageTitle>

<MudContainer MaxWidth=""MaxWidth.ExtraLarge"">
    <MudText Typo=""Typo.h4"" Class=""mb-4"">{{ReportName}} Report</MudText>
    
    <MudPaper Class=""pa-4 mb-4"">
        <MudGrid>
            <MudItem xs=""12"" md=""6"">
                <MudDatePicker Label=""Start Date"" @bind-Date=""_startDate"" />
            </MudItem>
            <MudItem xs=""12"" md=""6"">
                <MudDatePicker Label=""End Date"" @bind-Date=""_endDate"" />
            </MudItem>
            <MudItem xs=""12"">
                <MudButton Variant=""Variant.Filled""
                           Color=""Color.Primary""
                           StartIcon=""@Icons.Material.Filled.PlayArrow""
                           OnClick=""@GenerateReport""
                           Disabled=""@_loading"">
                    Generate Report
                </MudButton>
                <MudButton Variant=""Variant.Outlined""
                           Color=""Color.Secondary""
                           StartIcon=""@Icons.Material.Filled.Download""
                           OnClick=""@ExportReport""
                           Disabled=""@(_loading || _reportData == null)"">
                    Export
                </MudButton>
            </MudItem>
        </MudGrid>
    </MudPaper>
    
    @if (_loading)
    {
        <MudProgressCircular Indeterminate=""true"" />
    }
    else if (_reportData != null)
    {
        <MudPaper Class=""pa-4"">
            <MudTable Items=""@_reportData"" Dense=""@true"" Hover=""@true"">
                <HeaderContent>
                    @foreach (var field in _fields)
                    {
                        <MudTh>@field</MudTh>
                    }
                </HeaderContent>
                <RowTemplate>
                    @foreach (var field in _fields)
                    {
                        <MudTd>@GetFieldValue(context, field)</MudTd>
                    }
                </RowTemplate>
            </MudTable>
        </MudPaper>
    }
</MudContainer>

@code {
    private DateTime? _startDate = DateTime.Now.AddDays(-30);
    private DateTime? _endDate = DateTime.Now;
    private bool _loading = false;
    private object[]? _reportData;
    private readonly string[] _fields = { {{Fields}} };
    
    private async Task GenerateReport()
    {
        try
        {
            _loading = true;
            StateHasChanged();
            
            var parameters = new ReportParameters
            {
                StartDate = _startDate ?? DateTime.Now.AddDays(-30),
                EndDate = _endDate ?? DateTime.Now
            };
            
            var result = await ReportService.GenerateAsync(""{{ReportName}}"", parameters);
            
            if (result.Success)
            {
                _reportData = (object[])result.Data;
            }
            else
            {
                // Handle error
                _reportData = null;
            }
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
    
    private async Task ExportReport()
    {
        // Export implementation
        await Task.CompletedTask;
    }
    
    private string GetFieldValue(object item, string field)
    {
        // Get field value from item
        return item?.GetType().GetProperty(field)?.GetValue(item)?.ToString() ?? string.Empty;
    }
}";
    }

    #endregion
}