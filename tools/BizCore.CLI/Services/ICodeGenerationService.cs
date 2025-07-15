namespace BizCore.CLI.Services;

/// <summary>
/// Code generation service for rapid development
/// Generates Orleans grains, API controllers, UI components, and more
/// </summary>
public interface ICodeGenerationService
{
    Task CreateModuleAsync(string name, string type, bool withGrain, bool withApi, bool withUI);
    Task CreateGrainAsync(string name, string service, bool withState, bool withPersistence, bool withEvents);
    Task CreateIntegrationAsync(string provider, string service, string type);
    Task CreateEntityAsync(string name, string module, bool withGrain, bool withApi);
    Task CreateWorkflowAsync(string name, string description, string[] steps);
    Task CreateReportAsync(string name, string dataSource, string[] fields);
    Task CreateApiControllerAsync(string name, string module);
    Task CreateBlazorComponentAsync(string name, string module, string componentType);
}

/// <summary>
/// Code generation service implementation
/// </summary>
public class CodeGenerationService : ICodeGenerationService
{
    private readonly ILogger<CodeGenerationService> _logger;
    private readonly ITemplateService _templateService;

    public CodeGenerationService(ILogger<CodeGenerationService> logger, ITemplateService templateService)
    {
        _logger = logger;
        _templateService = templateService;
    }

    public async Task CreateModuleAsync(string name, string type, bool withGrain, bool withApi, bool withUI)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating module: {name}");
            
            var moduleConfig = new ModuleConfiguration
            {
                Name = name,
                Type = type,
                WithGrain = withGrain,
                WithApi = withApi,
                WithUI = withUI
            };

            // Create module directory structure
            await CreateModuleStructureAsync(moduleConfig);

            // Generate domain models
            await GenerateDomainModelsAsync(moduleConfig);

            // Generate Orleans grain if requested
            if (withGrain)
            {
                await GenerateGrainAsync(moduleConfig);
            }

            // Generate API controller if requested
            if (withApi)
            {
                await GenerateApiControllerAsync(moduleConfig);
            }

            // Generate Blazor components if requested
            if (withUI)
            {
                await GenerateBlazorComponentsAsync(moduleConfig);
            }

            // Generate tests
            await GenerateTestsAsync(moduleConfig);

            CliHelpers.WriteSuccess($"Module '{name}' created successfully!");
            CliHelpers.WriteInfo("Files created:");
            CliHelpers.WriteInfo($"  - Domain models in src/Shared/BizCore.Domain/{name}/");
            if (withGrain) CliHelpers.WriteInfo($"  - Orleans grain in src/Services/BizCore.{name}.Service/");
            if (withApi) CliHelpers.WriteInfo($"  - API controller in src/Infrastructure/BizCore.ApiGateway/Controllers/");
            if (withUI) CliHelpers.WriteInfo($"  - Blazor components in src/Frontend/BizCore.Web/Components/{name}/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create module {ModuleName}", name);
            CliHelpers.WriteError($"Failed to create module: {ex.Message}");
        }
    }

    public async Task CreateGrainAsync(string name, string service, bool withState, bool withPersistence, bool withEvents)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating Orleans grain: {name}");

            var grainConfig = new GrainConfiguration
            {
                Name = name,
                Service = service,
                WithState = withState,
                WithPersistence = withPersistence,
                WithEvents = withEvents
            };

            // Generate grain interface
            await GenerateGrainInterfaceAsync(grainConfig);

            // Generate grain implementation
            await GenerateGrainImplementationAsync(grainConfig);

            // Generate state class if requested
            if (withState)
            {
                await GenerateGrainStateAsync(grainConfig);
            }

            // Generate event classes if requested
            if (withEvents)
            {
                await GenerateGrainEventsAsync(grainConfig);
            }

            // Generate tests
            await GenerateGrainTestsAsync(grainConfig);

            CliHelpers.WriteSuccess($"Grain '{name}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create grain {GrainName}", name);
            CliHelpers.WriteError($"Failed to create grain: {ex.Message}");
        }
    }

    public async Task CreateIntegrationAsync(string provider, string service, string type)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating integration: {provider} for {service}");

            var integrationConfig = new IntegrationConfiguration
            {
                Provider = provider,
                Service = service,
                Type = type
            };

            // Generate integration service
            await GenerateIntegrationServiceAsync(integrationConfig);

            // Generate configuration
            await GenerateIntegrationConfigurationAsync(integrationConfig);

            // Generate tests
            await GenerateIntegrationTestsAsync(integrationConfig);

            CliHelpers.WriteSuccess($"Integration '{provider}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create integration {Provider}", provider);
            CliHelpers.WriteError($"Failed to create integration: {ex.Message}");
        }
    }

    public async Task CreateEntityAsync(string name, string module, bool withGrain, bool withApi)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating entity: {name}");

            var entityConfig = new EntityConfiguration
            {
                Name = name,
                Module = module,
                WithGrain = withGrain,
                WithApi = withApi
            };

            // Generate entity class
            await GenerateEntityClassAsync(entityConfig);

            // Generate DTOs
            await GenerateEntityDTOsAsync(entityConfig);

            // Generate grain if requested
            if (withGrain)
            {
                await GenerateEntityGrainAsync(entityConfig);
            }

            // Generate API endpoints if requested
            if (withApi)
            {
                await GenerateEntityApiAsync(entityConfig);
            }

            CliHelpers.WriteSuccess($"Entity '{name}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create entity {EntityName}", name);
            CliHelpers.WriteError($"Failed to create entity: {ex.Message}");
        }
    }

    public async Task CreateWorkflowAsync(string name, string description, string[] steps)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating workflow: {name}");

            var workflowConfig = new WorkflowConfiguration
            {
                Name = name,
                Description = description,
                Steps = steps
            };

            // Generate workflow definition
            await GenerateWorkflowDefinitionAsync(workflowConfig);

            // Generate workflow activities
            await GenerateWorkflowActivitiesAsync(workflowConfig);

            // Generate workflow tests
            await GenerateWorkflowTestsAsync(workflowConfig);

            CliHelpers.WriteSuccess($"Workflow '{name}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow {WorkflowName}", name);
            CliHelpers.WriteError($"Failed to create workflow: {ex.Message}");
        }
    }

    public async Task CreateReportAsync(string name, string dataSource, string[] fields)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating report: {name}");

            var reportConfig = new ReportConfiguration
            {
                Name = name,
                DataSource = dataSource,
                Fields = fields
            };

            // Generate report class
            await GenerateReportClassAsync(reportConfig);

            // Generate report query
            await GenerateReportQueryAsync(reportConfig);

            // Generate Blazor report component
            await GenerateReportComponentAsync(reportConfig);

            CliHelpers.WriteSuccess($"Report '{name}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create report {ReportName}", name);
            CliHelpers.WriteError($"Failed to create report: {ex.Message}");
        }
    }

    public async Task CreateApiControllerAsync(string name, string module)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating API controller: {name}");

            var controllerTemplate = await _templateService.GetTemplateAsync("ApiController");
            var controllerContent = controllerTemplate
                .Replace("{{ControllerName}}", name)
                .Replace("{{ModuleName}}", module)
                .Replace("{{EntityName}}", name.Replace("Controller", ""))
                .Replace("{{Namespace}}", $"BizCore.{module}.Api.Controllers");

            var controllerPath = Path.Combine("src", "Infrastructure", "BizCore.ApiGateway", "Controllers", $"{name}Controller.cs");
            await EnsureDirectoryExistsAsync(Path.GetDirectoryName(controllerPath));
            await File.WriteAllTextAsync(controllerPath, controllerContent);

            CliHelpers.WriteSuccess($"API controller '{name}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API controller {ControllerName}", name);
            CliHelpers.WriteError($"Failed to create API controller: {ex.Message}");
        }
    }

    public async Task CreateBlazorComponentAsync(string name, string module, string componentType)
    {
        try
        {
            CliHelpers.WriteInfo($"Creating Blazor component: {name}");

            var componentTemplate = await _templateService.GetTemplateAsync($"BlazorComponent_{componentType}");
            var componentContent = componentTemplate
                .Replace("{{ComponentName}}", name)
                .Replace("{{ModuleName}}", module)
                .Replace("{{Namespace}}", $"BizCore.Web.Components.{module}");

            var componentPath = Path.Combine("src", "Frontend", "BizCore.Web", "Components", module, $"{name}.razor");
            await EnsureDirectoryExistsAsync(Path.GetDirectoryName(componentPath));
            await File.WriteAllTextAsync(componentPath, componentContent);

            // Generate code-behind file
            var codeBehindTemplate = await _templateService.GetTemplateAsync($"BlazorComponentCodeBehind_{componentType}");
            var codeBehindContent = codeBehindTemplate
                .Replace("{{ComponentName}}", name)
                .Replace("{{ModuleName}}", module)
                .Replace("{{Namespace}}", $"BizCore.Web.Components.{module}");

            var codeBehindPath = Path.Combine("src", "Frontend", "BizCore.Web", "Components", module, $"{name}.razor.cs");
            await File.WriteAllTextAsync(codeBehindPath, codeBehindContent);

            CliHelpers.WriteSuccess($"Blazor component '{name}' created successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Blazor component {ComponentName}", name);
            CliHelpers.WriteError($"Failed to create Blazor component: {ex.Message}");
        }
    }

    private async Task CreateModuleStructureAsync(ModuleConfiguration config)
    {
        var basePath = Directory.GetCurrentDirectory();
        
        // Create domain directory
        var domainPath = Path.Combine(basePath, "src", "Shared", "BizCore.Domain", config.Name);
        Directory.CreateDirectory(domainPath);

        // Create service directory if grain is requested
        if (config.WithGrain)
        {
            var servicePath = Path.Combine(basePath, "src", "Services", $"BizCore.{config.Name}.Service");
            Directory.CreateDirectory(servicePath);
            Directory.CreateDirectory(Path.Combine(servicePath, "Grains"));
        }

        // Create UI directory if UI is requested
        if (config.WithUI)
        {
            var uiPath = Path.Combine(basePath, "src", "Frontend", "BizCore.Web", "Components", config.Name);
            Directory.CreateDirectory(uiPath);
            Directory.CreateDirectory(Path.Combine(uiPath, "Pages"));
            Directory.CreateDirectory(Path.Combine(uiPath, "Components"));
        }

        // Create test directory
        var testPath = Path.Combine(basePath, "tests", $"BizCore.{config.Name}.Tests");
        Directory.CreateDirectory(testPath);
    }

    private async Task GenerateDomainModelsAsync(ModuleConfiguration config)
    {
        var domainPath = Path.Combine("src", "Shared", "BizCore.Domain", config.Name);
        
        // Generate aggregate root
        var aggregateTemplate = await _templateService.GetTemplateAsync("AggregateRoot");
        var aggregateContent = aggregateTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.Domain.{config.Name}");

        await File.WriteAllTextAsync(Path.Combine(domainPath, $"{config.Name}.cs"), aggregateContent);

        // Generate value objects
        var valueObjectTemplate = await _templateService.GetTemplateAsync("ValueObject");
        var valueObjectContent = valueObjectTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.Domain.{config.Name}");

        await File.WriteAllTextAsync(Path.Combine(domainPath, $"{config.Name}Id.cs"), valueObjectContent);
    }

    private async Task GenerateGrainAsync(ModuleConfiguration config)
    {
        var grainPath = Path.Combine("src", "Services", $"BizCore.{config.Name}.Service", "Grains");
        
        // Generate grain interface
        var grainInterfaceTemplate = await _templateService.GetTemplateAsync("GrainInterface");
        var grainInterfaceContent = grainInterfaceTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.{config.Name}.Service.Grains");

        await File.WriteAllTextAsync(Path.Combine(grainPath, $"I{config.Name}Grain.cs"), grainInterfaceContent);

        // Generate grain implementation
        var grainTemplate = await _templateService.GetTemplateAsync("Grain");
        var grainContent = grainTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.{config.Name}.Service.Grains");

        await File.WriteAllTextAsync(Path.Combine(grainPath, $"{config.Name}Grain.cs"), grainContent);
    }

    private async Task GenerateApiControllerAsync(ModuleConfiguration config)
    {
        var controllerPath = Path.Combine("src", "Infrastructure", "BizCore.ApiGateway", "Controllers");
        
        var controllerTemplate = await _templateService.GetTemplateAsync("ApiController");
        var controllerContent = controllerTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{ControllerName}}", config.Name)
            .Replace("{{Namespace}}", "BizCore.ApiGateway.Controllers");

        await File.WriteAllTextAsync(Path.Combine(controllerPath, $"{config.Name}Controller.cs"), controllerContent);
    }

    private async Task GenerateBlazorComponentsAsync(ModuleConfiguration config)
    {
        var componentPath = Path.Combine("src", "Frontend", "BizCore.Web", "Components", config.Name);
        
        // Generate list component
        var listTemplate = await _templateService.GetTemplateAsync("BlazorListComponent");
        var listContent = listTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.Web.Components.{config.Name}");

        await File.WriteAllTextAsync(Path.Combine(componentPath, $"{config.Name}List.razor"), listContent);

        // Generate detail component
        var detailTemplate = await _templateService.GetTemplateAsync("BlazorDetailComponent");
        var detailContent = detailTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.Web.Components.{config.Name}");

        await File.WriteAllTextAsync(Path.Combine(componentPath, $"{config.Name}Detail.razor"), detailContent);
    }

    private async Task GenerateTestsAsync(ModuleConfiguration config)
    {
        var testPath = Path.Combine("tests", $"BizCore.{config.Name}.Tests");
        
        // Generate unit tests
        var unitTestTemplate = await _templateService.GetTemplateAsync("UnitTest");
        var unitTestContent = unitTestTemplate
            .Replace("{{ModuleName}}", config.Name)
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{Namespace}}", $"BizCore.{config.Name}.Tests");

        await File.WriteAllTextAsync(Path.Combine(testPath, $"{config.Name}Tests.cs"), unitTestContent);

        // Generate integration tests if grain is included
        if (config.WithGrain)
        {
            var integrationTestTemplate = await _templateService.GetTemplateAsync("IntegrationTest");
            var integrationTestContent = integrationTestTemplate
                .Replace("{{ModuleName}}", config.Name)
                .Replace("{{GrainName}}", config.Name)
                .Replace("{{Namespace}}", $"BizCore.{config.Name}.Tests");

            await File.WriteAllTextAsync(Path.Combine(testPath, $"{config.Name}GrainTests.cs"), integrationTestContent);
        }
    }

    private async Task GenerateGrainInterfaceAsync(GrainConfiguration config)
    {
        var interfacePath = Path.Combine("src", "Shared", "BizCore.Orleans.Contracts", config.Service);
        await EnsureDirectoryExistsAsync(interfacePath);

        var template = await _templateService.GetTemplateAsync("GrainInterface");
        var content = template
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{ServiceName}}", config.Service)
            .Replace("{{Namespace}}", $"BizCore.Orleans.Contracts.{config.Service}");

        await File.WriteAllTextAsync(Path.Combine(interfacePath, $"I{config.Name}Grain.cs"), content);
    }

    private async Task GenerateGrainImplementationAsync(GrainConfiguration config)
    {
        var grainPath = Path.Combine("src", "Services", $"BizCore.{config.Service}.Service", "Grains");
        await EnsureDirectoryExistsAsync(grainPath);

        var template = await _templateService.GetTemplateAsync("Grain");
        var content = template
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{ServiceName}}", config.Service)
            .Replace("{{Namespace}}", $"BizCore.{config.Service}.Service.Grains")
            .Replace("{{WithState}}", config.WithState.ToString().ToLower())
            .Replace("{{WithPersistence}}", config.WithPersistence.ToString().ToLower())
            .Replace("{{WithEvents}}", config.WithEvents.ToString().ToLower());

        await File.WriteAllTextAsync(Path.Combine(grainPath, $"{config.Name}Grain.cs"), content);
    }

    private async Task GenerateGrainStateAsync(GrainConfiguration config)
    {
        var statePath = Path.Combine("src", "Services", $"BizCore.{config.Service}.Service", "States");
        await EnsureDirectoryExistsAsync(statePath);

        var template = await _templateService.GetTemplateAsync("GrainState");
        var content = template
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{ServiceName}}", config.Service)
            .Replace("{{Namespace}}", $"BizCore.{config.Service}.Service.States");

        await File.WriteAllTextAsync(Path.Combine(statePath, $"{config.Name}State.cs"), content);
    }

    private async Task GenerateGrainEventsAsync(GrainConfiguration config)
    {
        var eventsPath = Path.Combine("src", "Services", $"BizCore.{config.Service}.Service", "Events");
        await EnsureDirectoryExistsAsync(eventsPath);

        var template = await _templateService.GetTemplateAsync("GrainEvents");
        var content = template
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{ServiceName}}", config.Service)
            .Replace("{{Namespace}}", $"BizCore.{config.Service}.Service.Events");

        await File.WriteAllTextAsync(Path.Combine(eventsPath, $"{config.Name}Events.cs"), content);
    }

    private async Task GenerateGrainTestsAsync(GrainConfiguration config)
    {
        var testPath = Path.Combine("tests", $"BizCore.{config.Service}.Tests");
        await EnsureDirectoryExistsAsync(testPath);

        var template = await _templateService.GetTemplateAsync("GrainTest");
        var content = template
            .Replace("{{GrainName}}", config.Name)
            .Replace("{{ServiceName}}", config.Service)
            .Replace("{{Namespace}}", $"BizCore.{config.Service}.Tests");

        await File.WriteAllTextAsync(Path.Combine(testPath, $"{config.Name}GrainTests.cs"), content);
    }

    private async Task GenerateIntegrationServiceAsync(IntegrationConfiguration config)
    {
        var integrationPath = Path.Combine("src", "Infrastructure", "BizCore.Integrations", config.Provider);
        await EnsureDirectoryExistsAsync(integrationPath);

        var template = await _templateService.GetTemplateAsync("IntegrationService");
        var content = template
            .Replace("{{Provider}}", config.Provider)
            .Replace("{{Service}}", config.Service)
            .Replace("{{Type}}", config.Type)
            .Replace("{{Namespace}}", $"BizCore.Integrations.{config.Provider}");

        await File.WriteAllTextAsync(Path.Combine(integrationPath, $"{config.Provider}Service.cs"), content);
    }

    private async Task GenerateIntegrationConfigurationAsync(IntegrationConfiguration config)
    {
        var configPath = Path.Combine("src", "Infrastructure", "BizCore.Integrations", config.Provider);
        
        var template = await _templateService.GetTemplateAsync("IntegrationConfiguration");
        var content = template
            .Replace("{{Provider}}", config.Provider)
            .Replace("{{Namespace}}", $"BizCore.Integrations.{config.Provider}");

        await File.WriteAllTextAsync(Path.Combine(configPath, $"{config.Provider}Configuration.cs"), content);
    }

    private async Task GenerateIntegrationTestsAsync(IntegrationConfiguration config)
    {
        var testPath = Path.Combine("tests", "BizCore.Integrations.Tests");
        await EnsureDirectoryExistsAsync(testPath);

        var template = await _templateService.GetTemplateAsync("IntegrationTest");
        var content = template
            .Replace("{{Provider}}", config.Provider)
            .Replace("{{Namespace}}", "BizCore.Integrations.Tests");

        await File.WriteAllTextAsync(Path.Combine(testPath, $"{config.Provider}Tests.cs"), content);
    }

    private async Task GenerateEntityClassAsync(EntityConfiguration config)
    {
        var entityPath = Path.Combine("src", "Shared", "BizCore.Domain", config.Module);
        await EnsureDirectoryExistsAsync(entityPath);

        var template = await _templateService.GetTemplateAsync("Entity");
        var content = template
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{ModuleName}}", config.Module)
            .Replace("{{Namespace}}", $"BizCore.Domain.{config.Module}");

        await File.WriteAllTextAsync(Path.Combine(entityPath, $"{config.Name}.cs"), content);
    }

    private async Task GenerateEntityDTOsAsync(EntityConfiguration config)
    {
        var dtoPath = Path.Combine("src", "Shared", "BizCore.Application", "DTOs", config.Module);
        await EnsureDirectoryExistsAsync(dtoPath);

        var template = await _templateService.GetTemplateAsync("EntityDTO");
        var content = template
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{ModuleName}}", config.Module)
            .Replace("{{Namespace}}", $"BizCore.Application.DTOs.{config.Module}");

        await File.WriteAllTextAsync(Path.Combine(dtoPath, $"{config.Name}Dto.cs"), content);
    }

    private async Task GenerateEntityGrainAsync(EntityConfiguration config)
    {
        var grainConfig = new GrainConfiguration
        {
            Name = config.Name,
            Service = config.Module,
            WithState = true,
            WithPersistence = true,
            WithEvents = false
        };

        await GenerateGrainInterfaceAsync(grainConfig);
        await GenerateGrainImplementationAsync(grainConfig);
        await GenerateGrainStateAsync(grainConfig);
    }

    private async Task GenerateEntityApiAsync(EntityConfiguration config)
    {
        var controllerPath = Path.Combine("src", "Infrastructure", "BizCore.ApiGateway", "Controllers");
        await EnsureDirectoryExistsAsync(controllerPath);

        var template = await _templateService.GetTemplateAsync("EntityApiController");
        var content = template
            .Replace("{{EntityName}}", config.Name)
            .Replace("{{ModuleName}}", config.Module)
            .Replace("{{Namespace}}", "BizCore.ApiGateway.Controllers");

        await File.WriteAllTextAsync(Path.Combine(controllerPath, $"{config.Name}Controller.cs"), content);
    }

    private async Task GenerateWorkflowDefinitionAsync(WorkflowConfiguration config)
    {
        var workflowPath = Path.Combine("src", "Infrastructure", "BizCore.Workflows", "Definitions");
        await EnsureDirectoryExistsAsync(workflowPath);

        var template = await _templateService.GetTemplateAsync("WorkflowDefinition");
        var content = template
            .Replace("{{WorkflowName}}", config.Name)
            .Replace("{{Description}}", config.Description)
            .Replace("{{Namespace}}", "BizCore.Workflows.Definitions");

        await File.WriteAllTextAsync(Path.Combine(workflowPath, $"{config.Name}Workflow.cs"), content);
    }

    private async Task GenerateWorkflowActivitiesAsync(WorkflowConfiguration config)
    {
        var activitiesPath = Path.Combine("src", "Infrastructure", "BizCore.Workflows", "Activities");
        await EnsureDirectoryExistsAsync(activitiesPath);

        foreach (var step in config.Steps)
        {
            var template = await _templateService.GetTemplateAsync("WorkflowActivity");
            var content = template
                .Replace("{{ActivityName}}", step)
                .Replace("{{WorkflowName}}", config.Name)
                .Replace("{{Namespace}}", "BizCore.Workflows.Activities");

            await File.WriteAllTextAsync(Path.Combine(activitiesPath, $"{step}Activity.cs"), content);
        }
    }

    private async Task GenerateWorkflowTestsAsync(WorkflowConfiguration config)
    {
        var testPath = Path.Combine("tests", "BizCore.Workflows.Tests");
        await EnsureDirectoryExistsAsync(testPath);

        var template = await _templateService.GetTemplateAsync("WorkflowTest");
        var content = template
            .Replace("{{WorkflowName}}", config.Name)
            .Replace("{{Namespace}}", "BizCore.Workflows.Tests");

        await File.WriteAllTextAsync(Path.Combine(testPath, $"{config.Name}WorkflowTests.cs"), content);
    }

    private async Task GenerateReportClassAsync(ReportConfiguration config)
    {
        var reportPath = Path.Combine("src", "Infrastructure", "BizCore.Reporting", "Reports");
        await EnsureDirectoryExistsAsync(reportPath);

        var template = await _templateService.GetTemplateAsync("ReportClass");
        var content = template
            .Replace("{{ReportName}}", config.Name)
            .Replace("{{DataSource}}", config.DataSource)
            .Replace("{{Namespace}}", "BizCore.Reporting.Reports");

        await File.WriteAllTextAsync(Path.Combine(reportPath, $"{config.Name}Report.cs"), content);
    }

    private async Task GenerateReportQueryAsync(ReportConfiguration config)
    {
        var queryPath = Path.Combine("src", "Infrastructure", "BizCore.Reporting", "Queries");
        await EnsureDirectoryExistsAsync(queryPath);

        var template = await _templateService.GetTemplateAsync("ReportQuery");
        var content = template
            .Replace("{{ReportName}}", config.Name)
            .Replace("{{DataSource}}", config.DataSource)
            .Replace("{{Fields}}", string.Join(", ", config.Fields))
            .Replace("{{Namespace}}", "BizCore.Reporting.Queries");

        await File.WriteAllTextAsync(Path.Combine(queryPath, $"{config.Name}Query.cs"), content);
    }

    private async Task GenerateReportComponentAsync(ReportConfiguration config)
    {
        var componentPath = Path.Combine("src", "Frontend", "BizCore.Web", "Components", "Reports");
        await EnsureDirectoryExistsAsync(componentPath);

        var template = await _templateService.GetTemplateAsync("ReportComponent");
        var content = template
            .Replace("{{ReportName}}", config.Name)
            .Replace("{{Fields}}", string.Join(", ", config.Fields))
            .Replace("{{Namespace}}", "BizCore.Web.Components.Reports");

        await File.WriteAllTextAsync(Path.Combine(componentPath, $"{config.Name}Report.razor"), content);
    }

    private async Task EnsureDirectoryExistsAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}

// Configuration classes
public class ModuleConfiguration
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool WithGrain { get; set; }
    public bool WithApi { get; set; }
    public bool WithUI { get; set; }
}

public class GrainConfiguration
{
    public string Name { get; set; }
    public string Service { get; set; }
    public bool WithState { get; set; }
    public bool WithPersistence { get; set; }
    public bool WithEvents { get; set; }
}

public class IntegrationConfiguration
{
    public string Provider { get; set; }
    public string Service { get; set; }
    public string Type { get; set; }
}

public class EntityConfiguration
{
    public string Name { get; set; }
    public string Module { get; set; }
    public bool WithGrain { get; set; }
    public bool WithApi { get; set; }
}

public class WorkflowConfiguration
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Steps { get; set; }
}

public class ReportConfiguration
{
    public string Name { get; set; }
    public string DataSource { get; set; }
    public string[] Fields { get; set; }
}