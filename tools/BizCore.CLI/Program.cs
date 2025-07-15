using System.CommandLine;
using System.CommandLine.Parsing;
using BizCore.CLI.Commands;
using BizCore.CLI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BizCore.CLI;

/// <summary>
/// BizCore CLI - The ultimate developer experience for ERP development
/// Enables rapid scaffolding, code generation, and project management
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        // Create root command
        var rootCommand = new RootCommand("🚀 BizCore CLI - The ultimate ERP development tool")
        {
            Name = "bizcore"
        };

        // Add global options
        var verboseOption = new Option<bool>("--verbose", "Enable verbose logging");
        var outputOption = new Option<string>("--output", "Output directory");
        
        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddGlobalOption(outputOption);

        // Add commands
        var serviceProvider = host.Services;
        
        rootCommand.AddCommand(CreateProjectCommand(serviceProvider));
        rootCommand.AddCommand(CreateModuleCommand(serviceProvider));
        rootCommand.AddCommand(CreatePluginCommand(serviceProvider));
        rootCommand.AddCommand(CreateGrainCommand(serviceProvider));
        rootCommand.AddCommand(CreateIntegrationCommand(serviceProvider));
        rootCommand.AddCommand(CreateMigrationCommand(serviceProvider));
        rootCommand.AddCommand(CreateTemplateCommand(serviceProvider));
        rootCommand.AddCommand(CreateDeployCommand(serviceProvider));
        rootCommand.AddCommand(CreateMarketplaceCommand(serviceProvider));

        // Parse and execute
        try
        {
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "CLI execution failed");
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
            
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register core services
                services.AddSingleton<ITemplateService, TemplateService>();
                services.AddSingleton<ICodeGenerationService, CodeGenerationService>();
                services.AddSingleton<IProjectService, ProjectService>();
                services.AddSingleton<IPluginService, PluginService>();
                services.AddSingleton<IMigrationService, MigrationService>();
                services.AddSingleton<IMarketplaceService, MarketplaceService>();
                services.AddSingleton<IDeploymentService, DeploymentService>();
                
                // Register HTTP client for marketplace
                services.AddHttpClient();
                
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });

    private static Command CreateProjectCommand(IServiceProvider services)
    {
        var projectCommand = new Command("create", "Create new BizCore project");
        
        var nameOption = new Option<string>("--name", "Project name") { IsRequired = true };
        var templateOption = new Option<string>("--template", "Project template") { IsRequired = false };
        var outputOption = new Option<string>("--output", "Output directory") { IsRequired = false };
        var forceOption = new Option<bool>("--force", "Force creation even if directory exists");
        
        projectCommand.AddOption(nameOption);
        projectCommand.AddOption(templateOption);
        projectCommand.AddOption(outputOption);
        projectCommand.AddOption(forceOption);

        projectCommand.SetHandler(async (name, template, output, force) =>
        {
            var projectService = services.GetRequiredService<IProjectService>();
            await projectService.CreateProjectAsync(name, template, output, force);
        }, nameOption, templateOption, outputOption, forceOption);

        return projectCommand;
    }

    private static Command CreateModuleCommand(IServiceProvider services)
    {
        var moduleCommand = new Command("module", "Create new business module");
        
        var createCommand = new Command("create", "Create new module");
        var nameOption = new Option<string>("--name", "Module name") { IsRequired = true };
        var typeOption = new Option<string>("--type", "Module type (accounting, inventory, sales, etc.)");
        var grainOption = new Option<bool>("--with-grain", "Include Orleans grain");
        var apiOption = new Option<bool>("--with-api", "Include API endpoints");
        var uiOption = new Option<bool>("--with-ui", "Include Blazor UI components");
        
        createCommand.AddOption(nameOption);
        createCommand.AddOption(typeOption);
        createCommand.AddOption(grainOption);
        createCommand.AddOption(apiOption);
        createCommand.AddOption(uiOption);

        createCommand.SetHandler(async (name, type, withGrain, withApi, withUi) =>
        {
            var codeGenService = services.GetRequiredService<ICodeGenerationService>();
            await codeGenService.CreateModuleAsync(name, type, withGrain, withApi, withUi);
        }, nameOption, typeOption, grainOption, apiOption, uiOption);

        moduleCommand.AddCommand(createCommand);
        return moduleCommand;
    }

    private static Command CreatePluginCommand(IServiceProvider services)
    {
        var pluginCommand = new Command("plugin", "Plugin management");
        
        // Create plugin command
        var createCommand = new Command("create", "Create new plugin");
        var nameOption = new Option<string>("--name", "Plugin name") { IsRequired = true };
        var categoryOption = new Option<string>("--category", "Plugin category");
        var templateOption = new Option<string>("--template", "Plugin template");
        
        createCommand.AddOption(nameOption);
        createCommand.AddOption(categoryOption);
        createCommand.AddOption(templateOption);

        createCommand.SetHandler(async (name, category, template) =>
        {
            var pluginService = services.GetRequiredService<IPluginService>();
            await pluginService.CreatePluginAsync(name, category, template);
        }, nameOption, categoryOption, templateOption);

        // Install plugin command
        var installCommand = new Command("install", "Install plugin from marketplace");
        var pluginIdOption = new Option<string>("--id", "Plugin ID") { IsRequired = true };
        var versionOption = new Option<string>("--version", "Plugin version");
        
        installCommand.AddOption(pluginIdOption);
        installCommand.AddOption(versionOption);

        installCommand.SetHandler(async (pluginId, version) =>
        {
            var pluginService = services.GetRequiredService<IPluginService>();
            await pluginService.InstallPluginAsync(pluginId, version);
        }, pluginIdOption, versionOption);

        // List plugins command
        var listCommand = new Command("list", "List installed plugins");
        listCommand.SetHandler(async () =>
        {
            var pluginService = services.GetRequiredService<IPluginService>();
            await pluginService.ListPluginsAsync();
        });

        // Search plugins command
        var searchCommand = new Command("search", "Search plugins in marketplace");
        var queryOption = new Option<string>("--query", "Search query") { IsRequired = true };
        
        searchCommand.AddOption(queryOption);
        searchCommand.SetHandler(async (query) =>
        {
            var pluginService = services.GetRequiredService<IPluginService>();
            await pluginService.SearchPluginsAsync(query);
        }, queryOption);

        pluginCommand.AddCommand(createCommand);
        pluginCommand.AddCommand(installCommand);
        pluginCommand.AddCommand(listCommand);
        pluginCommand.AddCommand(searchCommand);

        return pluginCommand;
    }

    private static Command CreateGrainCommand(IServiceProvider services)
    {
        var grainCommand = new Command("grain", "Generate Orleans grain");
        
        var nameOption = new Option<string>("--name", "Grain name") { IsRequired = true };
        var serviceOption = new Option<string>("--service", "Target service") { IsRequired = true };
        var stateOption = new Option<bool>("--with-state", "Include grain state");
        var persistenceOption = new Option<bool>("--with-persistence", "Include persistence");
        var eventsOption = new Option<bool>("--with-events", "Include event sourcing");
        
        grainCommand.AddOption(nameOption);
        grainCommand.AddOption(serviceOption);
        grainCommand.AddOption(stateOption);
        grainCommand.AddOption(persistenceOption);
        grainCommand.AddOption(eventsOption);

        grainCommand.SetHandler(async (name, service, withState, withPersistence, withEvents) =>
        {
            var codeGenService = services.GetRequiredService<ICodeGenerationService>();
            await codeGenService.CreateGrainAsync(name, service, withState, withPersistence, withEvents);
        }, nameOption, serviceOption, stateOption, persistenceOption, eventsOption);

        return grainCommand;
    }

    private static Command CreateIntegrationCommand(IServiceProvider services)
    {
        var integrationCommand = new Command("integration", "Add third-party integration");
        
        var providerOption = new Option<string>("--provider", "Integration provider (stripe, paypal, etc.)") { IsRequired = true };
        var serviceOption = new Option<string>("--service", "Target service") { IsRequired = true };
        var typeOption = new Option<string>("--type", "Integration type (payment, shipping, etc.)");
        
        integrationCommand.AddOption(providerOption);
        integrationCommand.AddOption(serviceOption);
        integrationCommand.AddOption(typeOption);

        integrationCommand.SetHandler(async (provider, service, type) =>
        {
            var codeGenService = services.GetRequiredService<ICodeGenerationService>();
            await codeGenService.CreateIntegrationAsync(provider, service, type);
        }, providerOption, serviceOption, typeOption);

        return integrationCommand;
    }

    private static Command CreateMigrationCommand(IServiceProvider services)
    {
        var migrationCommand = new Command("migrate", "Migrate from existing ERP systems");
        
        var sourceOption = new Option<string>("--source", "Source ERP system (sap, dynamics, etc.)") { IsRequired = true };
        var connectionOption = new Option<string>("--connection", "Source connection string") { IsRequired = true };
        var modulesOption = new Option<string[]>("--modules", "Modules to migrate");
        var dryRunOption = new Option<bool>("--dry-run", "Preview migration without executing");
        
        migrationCommand.AddOption(sourceOption);
        migrationCommand.AddOption(connectionOption);
        migrationCommand.AddOption(modulesOption);
        migrationCommand.AddOption(dryRunOption);

        migrationCommand.SetHandler(async (source, connection, modules, dryRun) =>
        {
            var migrationService = services.GetRequiredService<IMigrationService>();
            await migrationService.MigrateAsync(source, connection, modules, dryRun);
        }, sourceOption, connectionOption, modulesOption, dryRunOption);

        return migrationCommand;
    }

    private static Command CreateTemplateCommand(IServiceProvider services)
    {
        var templateCommand = new Command("template", "Template management");
        
        // List templates
        var listCommand = new Command("list", "List available templates");
        listCommand.SetHandler(async () =>
        {
            var templateService = services.GetRequiredService<ITemplateService>();
            await templateService.ListTemplatesAsync();
        });

        // Install template
        var installCommand = new Command("install", "Install template");
        var sourceOption = new Option<string>("--source", "Template source (git, local, marketplace)") { IsRequired = true };
        var nameOption = new Option<string>("--name", "Template name") { IsRequired = true };
        
        installCommand.AddOption(sourceOption);
        installCommand.AddOption(nameOption);

        installCommand.SetHandler(async (source, name) =>
        {
            var templateService = services.GetRequiredService<ITemplateService>();
            await templateService.InstallTemplateAsync(source, name);
        }, sourceOption, nameOption);

        templateCommand.AddCommand(listCommand);
        templateCommand.AddCommand(installCommand);

        return templateCommand;
    }

    private static Command CreateDeployCommand(IServiceProvider services)
    {
        var deployCommand = new Command("deploy", "Deploy BizCore application");
        
        var targetOption = new Option<string>("--target", "Deployment target (local, docker, k8s, azure)") { IsRequired = true };
        var environmentOption = new Option<string>("--environment", "Environment (dev, staging, prod)") { IsRequired = true };
        var configOption = new Option<string>("--config", "Deployment configuration file");
        var watchOption = new Option<bool>("--watch", "Watch for changes and redeploy");
        
        deployCommand.AddOption(targetOption);
        deployCommand.AddOption(environmentOption);
        deployCommand.AddOption(configOption);
        deployCommand.AddOption(watchOption);

        deployCommand.SetHandler(async (target, environment, config, watch) =>
        {
            var deploymentService = services.GetRequiredService<IDeploymentService>();
            await deploymentService.DeployAsync(target, environment, config, watch);
        }, targetOption, environmentOption, configOption, watchOption);

        return deployCommand;
    }

    private static Command CreateMarketplaceCommand(IServiceProvider services)
    {
        var marketplaceCommand = new Command("marketplace", "Marketplace operations");
        
        // Publish plugin
        var publishCommand = new Command("publish", "Publish plugin to marketplace");
        var pathOption = new Option<string>("--path", "Plugin path") { IsRequired = true };
        var tokenOption = new Option<string>("--token", "Authentication token");
        
        publishCommand.AddOption(pathOption);
        publishCommand.AddOption(tokenOption);

        publishCommand.SetHandler(async (path, token) =>
        {
            var marketplaceService = services.GetRequiredService<IMarketplaceService>();
            await marketplaceService.PublishPluginAsync(path, token);
        }, pathOption, tokenOption);

        // Login
        var loginCommand = new Command("login", "Login to marketplace");
        var usernameOption = new Option<string>("--username", "Username") { IsRequired = true };
        var passwordOption = new Option<string>("--password", "Password") { IsRequired = true };
        
        loginCommand.AddOption(usernameOption);
        loginCommand.AddOption(passwordOption);

        loginCommand.SetHandler(async (username, password) =>
        {
            var marketplaceService = services.GetRequiredService<IMarketplaceService>();
            await marketplaceService.LoginAsync(username, password);
        }, usernameOption, passwordOption);

        marketplaceCommand.AddCommand(publishCommand);
        marketplaceCommand.AddCommand(loginCommand);

        return marketplaceCommand;
    }
}

/// <summary>
/// CLI utilities and helpers
/// </summary>
public static class CliHelpers
{
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ResetColor();
    }

    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ️  {message}");
        Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️  {message}");
        Console.ResetColor();
    }

    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {message}");
        Console.ResetColor();
    }

    public static void WriteLogo()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(@"
██████╗ ██╗███████╗ ██████╗ ██████╗ ██████╗ ███████╗
██╔══██╗██║╚══███╔╝██╔════╝██╔═══██╗██╔══██╗██╔════╝
██████╔╝██║  ███╔╝ ██║     ██║   ██║██████╔╝█████╗  
██╔══██╗██║ ███╔╝  ██║     ██║   ██║██╔══██╗██╔══╝  
██████╔╝██║███████╗╚██████╗╚██████╔╝██║  ██║███████╗
╚═════╝ ╚═╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝
                                                     
🚀 The Ultimate ERP Development Experience
");
        Console.ResetColor();
    }
}