using BizCore.CLI.Models;

namespace BizCore.CLI.Services;

/// <summary>
/// Project creation and management service
/// Provides rapid scaffolding for new BizCore projects
/// </summary>
public interface IProjectService
{
    Task CreateProjectAsync(string name, string template, string outputPath, bool force);
    Task<IEnumerable<ProjectTemplate>> GetAvailableTemplatesAsync();
    Task<ProjectInfo> GetProjectInfoAsync(string path);
    Task InitializeProjectAsync(string path);
    Task UpdateProjectAsync(string path, string targetVersion);
}

/// <summary>
/// Project service implementation
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ILogger<ProjectService> _logger;
    private readonly ITemplateService _templateService;
    private readonly HttpClient _httpClient;

    public ProjectService(ILogger<ProjectService> logger, ITemplateService templateService, HttpClient httpClient)
    {
        _logger = logger;
        _templateService = templateService;
        _httpClient = httpClient;
    }

    public async Task CreateProjectAsync(string name, string template, string outputPath, bool force)
    {
        try
        {
            CliHelpers.WriteLogo();
            CliHelpers.WriteInfo($"Creating new BizCore project: {name}");

            // Determine output path
            var projectPath = Path.Combine(outputPath ?? Directory.GetCurrentDirectory(), name);
            
            if (Directory.Exists(projectPath) && !force)
            {
                CliHelpers.WriteError($"Directory {projectPath} already exists. Use --force to overwrite.");
                return;
            }

            if (Directory.Exists(projectPath) && force)
            {
                Directory.Delete(projectPath, true);
            }

            // Create project directory
            Directory.CreateDirectory(projectPath);

            // Get template
            var selectedTemplate = await SelectTemplateAsync(template);
            if (selectedTemplate == null)
            {
                CliHelpers.WriteError($"Template '{template}' not found. Use 'bizcore template list' to see available templates.");
                return;
            }

            CliHelpers.WriteInfo($"Using template: {selectedTemplate.Name}");

            // Create project structure
            await CreateProjectStructureAsync(projectPath, selectedTemplate, name);

            // Generate configuration files
            await GenerateConfigurationFilesAsync(projectPath, name);

            // Generate Docker files
            await GenerateDockerFilesAsync(projectPath, name);

            // Generate Kubernetes manifests
            await GenerateKubernetesManifestsAsync(projectPath, name);

            // Generate CI/CD pipelines
            await GenerateCICDPipelinesAsync(projectPath, name);

            // Initialize git repository
            await InitializeGitRepositoryAsync(projectPath);

            // Restore NuGet packages
            await RestorePackagesAsync(projectPath);

            CliHelpers.WriteSuccess($"Project '{name}' created successfully!");
            CliHelpers.WriteInfo($"Location: {projectPath}");
            CliHelpers.WriteInfo("Next steps:");
            CliHelpers.WriteInfo("  1. cd " + name);
            CliHelpers.WriteInfo("  2. dotnet run");
            CliHelpers.WriteInfo("  3. Open https://localhost:5001 in your browser");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project {ProjectName}", name);
            CliHelpers.WriteError($"Failed to create project: {ex.Message}");
        }
    }

    public async Task<IEnumerable<ProjectTemplate>> GetAvailableTemplatesAsync()
    {
        var templates = new List<ProjectTemplate>
        {
            new ProjectTemplate
            {
                Id = "basic",
                Name = "Basic ERP",
                Description = "Basic ERP with core modules (Accounting, Inventory, Sales)",
                Category = "Starter",
                Features = new[] { "Accounting", "Inventory", "Sales", "Basic UI" },
                IsDefault = true
            },
            new ProjectTemplate
            {
                Id = "retail",
                Name = "Retail Management",
                Description = "Complete retail management solution with POS, inventory, and customer management",
                Category = "Industry",
                Features = new[] { "POS", "Inventory", "Customer Management", "Loyalty Program", "Multi-store" }
            },
            new ProjectTemplate
            {
                Id = "manufacturing",
                Name = "Manufacturing ERP",
                Description = "Manufacturing ERP with MRP, production planning, and quality control",
                Category = "Industry",
                Features = new[] { "MRP", "Production Planning", "Quality Control", "Supply Chain", "Costing" }
            },
            new ProjectTemplate
            {
                Id = "healthcare",
                Name = "Healthcare Management",
                Description = "Healthcare management system with patient records, appointments, and billing",
                Category = "Industry",
                Features = new[] { "Patient Management", "Appointments", "Medical Records", "Billing", "Compliance" }
            },
            new ProjectTemplate
            {
                Id = "construction",
                Name = "Construction Management",
                Description = "Construction project management with resource planning and cost tracking",
                Category = "Industry",
                Features = new[] { "Project Management", "Resource Planning", "Cost Tracking", "Scheduling", "Reporting" }
            },
            new ProjectTemplate
            {
                Id = "professional-services",
                Name = "Professional Services",
                Description = "Professional services ERP with project management, time tracking, and billing",
                Category = "Industry",
                Features = new[] { "Project Management", "Time Tracking", "Billing", "Resource Management", "CRM" }
            },
            new ProjectTemplate
            {
                Id = "saas-platform",
                Name = "SaaS Platform",
                Description = "Multi-tenant SaaS platform template with subscription management",
                Category = "Platform",
                Features = new[] { "Multi-tenant", "Subscription Management", "Usage Tracking", "API Gateway", "Admin Portal" }
            },
            new ProjectTemplate
            {
                Id = "microservices",
                Name = "Microservices Architecture",
                Description = "Advanced microservices template with full Orleans cluster and event sourcing",
                Category = "Advanced",
                Features = new[] { "Orleans Cluster", "Event Sourcing", "CQRS", "Saga Orchestration", "Distributed Cache" }
            }
        };

        return templates;
    }

    public async Task<ProjectInfo> GetProjectInfoAsync(string path)
    {
        try
        {
            var solutionFile = Directory.GetFiles(path, "*.sln").FirstOrDefault();
            if (solutionFile == null)
            {
                return null;
            }

            var solutionContent = await File.ReadAllTextAsync(solutionFile);
            var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);

            return new ProjectInfo
            {
                Name = Path.GetFileNameWithoutExtension(solutionFile),
                Path = path,
                SolutionFile = solutionFile,
                ProjectFiles = projectFiles,
                IsBizCoreProject = solutionContent.Contains("BizCore."),
                Version = await GetProjectVersionAsync(path)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project info for path {Path}", path);
            return null;
        }
    }

    public async Task InitializeProjectAsync(string path)
    {
        try
        {
            CliHelpers.WriteInfo("Initializing BizCore project...");

            // Check if already initialized
            var projectInfo = await GetProjectInfoAsync(path);
            if (projectInfo?.IsBizCoreProject == true)
            {
                CliHelpers.WriteWarning("Project is already initialized as BizCore project");
                return;
            }

            // Add BizCore packages to existing project
            var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
            foreach (var projectFile in projectFiles)
            {
                await AddBizCorePackagesAsync(projectFile);
            }

            // Create BizCore configuration
            await CreateBizCoreConfigurationAsync(path);

            CliHelpers.WriteSuccess("Project initialized successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize project at {Path}", path);
            CliHelpers.WriteError($"Failed to initialize project: {ex.Message}");
        }
    }

    public async Task UpdateProjectAsync(string path, string targetVersion)
    {
        try
        {
            CliHelpers.WriteInfo($"Updating project to version {targetVersion}...");

            var projectInfo = await GetProjectInfoAsync(path);
            if (projectInfo == null)
            {
                CliHelpers.WriteError("Not a valid project directory");
                return;
            }

            // Update package references
            foreach (var projectFile in projectInfo.ProjectFiles)
            {
                await UpdatePackageReferencesAsync(projectFile, targetVersion);
            }

            // Update configuration files
            await UpdateConfigurationFilesAsync(path, targetVersion);

            CliHelpers.WriteSuccess($"Project updated to version {targetVersion}!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project at {Path}", path);
            CliHelpers.WriteError($"Failed to update project: {ex.Message}");
        }
    }

    private async Task<ProjectTemplate> SelectTemplateAsync(string template)
    {
        var templates = await GetAvailableTemplatesAsync();
        
        if (string.IsNullOrEmpty(template))
        {
            return templates.FirstOrDefault(t => t.IsDefault);
        }

        return templates.FirstOrDefault(t => 
            t.Id.Equals(template, StringComparison.OrdinalIgnoreCase) ||
            t.Name.Equals(template, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CreateProjectStructureAsync(string projectPath, ProjectTemplate template, string projectName)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", template.Id);
        
        if (Directory.Exists(templatePath))
        {
            // Copy from local template
            await CopyDirectoryAsync(templatePath, projectPath);
        }
        else
        {
            // Generate from built-in template
            await GenerateFromBuiltInTemplateAsync(projectPath, template, projectName);
        }

        // Replace template placeholders
        await ReplaceTemplatePlaceholdersAsync(projectPath, projectName);
    }

    private async Task GenerateFromBuiltInTemplateAsync(string projectPath, ProjectTemplate template, string projectName)
    {
        // Create solution file
        var solutionContent = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1

Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}.Web"", ""src\\{projectName}.Web\\{projectName}.Web.csproj"", ""{{11111111-1111-1111-1111-111111111111}}""
EndProject
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}.API"", ""src\\{projectName}.API\\{projectName}.API.csproj"", ""{{22222222-2222-2222-2222-222222222222}}""
EndProject
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}.Services"", ""src\\{projectName}.Services\\{projectName}.Services.csproj"", ""{{33333333-3333-3333-3333-333333333333}}""
EndProject

Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{11111111-1111-1111-1111-111111111111}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{11111111-1111-1111-1111-111111111111}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{11111111-1111-1111-1111-111111111111}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{11111111-1111-1111-1111-111111111111}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{22222222-2222-2222-2222-222222222222}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{22222222-2222-2222-2222-222222222222}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{22222222-2222-2222-2222-222222222222}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{22222222-2222-2222-2222-222222222222}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{33333333-3333-3333-3333-333333333333}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{33333333-3333-3333-3333-333333333333}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{33333333-3333-3333-3333-333333333333}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{33333333-3333-3333-3333-333333333333}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
";

        await File.WriteAllTextAsync(Path.Combine(projectPath, $"{projectName}.sln"), solutionContent);

        // Create project directories
        Directory.CreateDirectory(Path.Combine(projectPath, "src"));
        Directory.CreateDirectory(Path.Combine(projectPath, "tests"));
        Directory.CreateDirectory(Path.Combine(projectPath, "docs"));
        Directory.CreateDirectory(Path.Combine(projectPath, "scripts"));

        // Create basic project files based on template
        await CreateProjectFilesAsync(projectPath, template, projectName);
    }

    private async Task CreateProjectFilesAsync(string projectPath, ProjectTemplate template, string projectName)
    {
        // Create web project
        var webProjectPath = Path.Combine(projectPath, "src", $"{projectName}.Web");
        Directory.CreateDirectory(webProjectPath);

        var webProjectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""BizCore.Web"" Version=""1.0.0"" />
    <PackageReference Include=""MudBlazor"" Version=""6.11.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly.Server"" Version=""8.0.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\\{projectName}.Services\\{projectName}.Services.csproj"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(webProjectPath, $"{projectName}.Web.csproj"), webProjectContent);

        // Create Program.cs
        var programContent = $@"using BizCore.Web;
using MudBlazor.Services;
using {projectName}.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Add BizCore services
builder.Services.AddBizCore();

// Add application services
builder.Services.Add{projectName}Services();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage(""/_Host"");

app.Run();
";

        await File.WriteAllTextAsync(Path.Combine(webProjectPath, "Program.cs"), programContent);
    }

    private async Task GenerateConfigurationFilesAsync(string projectPath, string projectName)
    {
        // appsettings.json
        var appSettingsContent = $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""Server=localhost;Database={projectName};Trusted_Connection=true;TrustServerCertificate=true;"",
    ""Redis"": ""localhost:6379""
  }},
  ""Orleans"": {{
    ""ClusterId"": ""{projectName.ToLower()}"",
    ""ServiceId"": ""{projectName}""
  }},
  ""BizCore"": {{
    ""TenantId"": ""default"",
    ""CompanyName"": ""{projectName} Corp"",
    ""MultiTenant"": false
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*""
}}";

        await File.WriteAllTextAsync(Path.Combine(projectPath, "appsettings.json"), appSettingsContent);

        // appsettings.Development.json
        var appSettingsDevContent = $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""Server=localhost;Database={projectName}_Dev;Trusted_Connection=true;TrustServerCertificate=true;""
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Debug"",
      ""System"": ""Information"",
      ""Microsoft"": ""Information""
    }}
  }}
}}";

        await File.WriteAllTextAsync(Path.Combine(projectPath, "appsettings.Development.json"), appSettingsDevContent);
    }

    private async Task GenerateDockerFilesAsync(string projectPath, string projectName)
    {
        var dockerfileContent = $@"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""{projectName}.sln"", "".""]
COPY [""src/{projectName}.Web/{projectName}.Web.csproj"", ""src/{projectName}.Web/""]
COPY [""src/{projectName}.Services/{projectName}.Services.csproj"", ""src/{projectName}.Services/""]
RUN dotnet restore

COPY . .
WORKDIR ""/src/src/{projectName}.Web""
RUN dotnet build ""{projectName}.Web.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{projectName}.Web.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{projectName}.Web.dll""]
";

        await File.WriteAllTextAsync(Path.Combine(projectPath, "Dockerfile"), dockerfileContent);

        // Docker Compose
        var dockerComposeContent = $@"version: '3.8'

services:
  {projectName.ToLower()}-web:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - ""5000:80""
      - ""5001:443""
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=password
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    volumes:
      - ~/.aspnet/https:/https:ro
    depends_on:
      - sqlserver
      - redis

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Passw0rd
    ports:
      - ""1433:1433""
    volumes:
      - sqlserver_data:/var/opt/mssql

  redis:
    image: redis:7-alpine
    ports:
      - ""6379:6379""
    volumes:
      - redis_data:/data

volumes:
  sqlserver_data:
  redis_data:
";

        await File.WriteAllTextAsync(Path.Combine(projectPath, "docker-compose.yml"), dockerComposeContent);
    }

    private async Task GenerateKubernetesManifestsAsync(string projectPath, string projectName)
    {
        var k8sPath = Path.Combine(projectPath, "k8s");
        Directory.CreateDirectory(k8sPath);

        var deploymentContent = $@"apiVersion: apps/v1
kind: Deployment
metadata:
  name: {projectName.ToLower()}-web
spec:
  replicas: 3
  selector:
    matchLabels:
      app: {projectName.ToLower()}-web
  template:
    metadata:
      labels:
        app: {projectName.ToLower()}-web
    spec:
      containers:
      - name: web
        image: {projectName.ToLower()}-web:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: {projectName.ToLower()}-secrets
              key: connection-string
";

        await File.WriteAllTextAsync(Path.Combine(k8sPath, "deployment.yaml"), deploymentContent);
    }

    private async Task GenerateCICDPipelinesAsync(string projectPath, string projectName)
    {
        var githubPath = Path.Combine(projectPath, ".github", "workflows");
        Directory.CreateDirectory(githubPath);

        var cicdContent = $@"name: {projectName} CI/CD

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Build Docker image
      run: docker build -t {projectName.ToLower()}:${{{{ github.sha }}}} .
    
    - name: Deploy to staging
      if: github.ref == 'refs/heads/develop'
      run: |
        echo ""Deploy to staging""
        # Add your deployment commands here
    
    - name: Deploy to production
      if: github.ref == 'refs/heads/main'
      run: |
        echo ""Deploy to production""
        # Add your deployment commands here
";

        await File.WriteAllTextAsync(Path.Combine(githubPath, "ci-cd.yml"), cicdContent);
    }

    private async Task InitializeGitRepositoryAsync(string projectPath)
    {
        var gitignoreContent = @"
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio files
.vs/
*.suo
*.user
*.userosscache
*.sln.docstates

# NuGet
*.nupkg
*.snupkg
.nuget/

# Docker
.dockerignore

# Environment variables
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# Logs
*.log
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Runtime data
pids
*.pid
*.seed
*.pid.lock

# Coverage directory used by tools like istanbul
coverage/
*.lcov

# nyc test coverage
.nyc_output

# Dependency directories
node_modules/
jspm_packages/

# Optional npm cache directory
.npm

# Optional eslint cache
.eslintcache

# Microbundle cache
.rpt2_cache/
.rts2_cache_cjs/
.rts2_cache_es/
.rts2_cache_umd/

# Optional REPL history
.node_repl_history

# Output of 'npm pack'
*.tgz

# Yarn Integrity file
.yarn-integrity

# dotenv environment variables file
.env
.env.test

# parcel-bundler cache (https://parceljs.org/)
.cache
.parcel-cache

# Next.js build output
.next

# Nuxt.js build / generate output
.nuxt
dist

# Gatsby files
.cache/
public

# Storybook build outputs
.out
.storybook-out

# Temporary folders
tmp/
temp/

# Editor directories and files
.vscode/*
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/launch.json
!.vscode/extensions.json
*.swp
*.swo
*~

# OS generated files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
";

        await File.WriteAllTextAsync(Path.Combine(projectPath, ".gitignore"), gitignoreContent);
    }

    private async Task RestorePackagesAsync(string projectPath)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "restore",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processStartInfo);
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                CliHelpers.WriteInfo("Packages restored successfully");
            }
            else
            {
                CliHelpers.WriteWarning("Package restore completed with warnings");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore packages");
            CliHelpers.WriteWarning("Could not restore packages automatically. Run 'dotnet restore' manually.");
        }
    }

    private async Task ReplaceTemplatePlaceholdersAsync(string projectPath, string projectName)
    {
        var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains("node_modules"))
            .Where(f => IsTextFile(f));

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var updatedContent = content
                .Replace("{{ProjectName}}", projectName)
                .Replace("{{ProjectNameLower}}", projectName.ToLower())
                .Replace("{{ProjectNameUpper}}", projectName.ToUpper())
                .Replace("{{Year}}", DateTime.Now.Year.ToString())
                .Replace("{{Date}}", DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("{{Guid}}", Guid.NewGuid().ToString());

            if (updatedContent != content)
            {
                await File.WriteAllTextAsync(file, updatedContent);
            }
        }
    }

    private bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".cs" or ".razor" or ".html" or ".css" or ".js" or ".json" or ".xml" or ".yml" or ".yaml" or ".md" or ".txt" => true,
            _ => false
        };
    }

    private async Task<string> GetProjectVersionAsync(string projectPath)
    {
        try
        {
            var versionFile = Path.Combine(projectPath, "version.txt");
            if (File.Exists(versionFile))
            {
                return await File.ReadAllTextAsync(versionFile);
            }
            return "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    private async Task AddBizCorePackagesAsync(string projectFile)
    {
        // Implementation for adding BizCore packages to existing project
        // This would modify the .csproj file to include BizCore packages
    }

    private async Task CreateBizCoreConfigurationAsync(string projectPath)
    {
        // Implementation for creating BizCore configuration files
        // This would create appsettings.json with BizCore configuration
    }

    private async Task UpdatePackageReferencesAsync(string projectFile, string targetVersion)
    {
        // Implementation for updating package references to target version
        // This would modify .csproj files to update BizCore package versions
    }

    private async Task UpdateConfigurationFilesAsync(string projectPath, string targetVersion)
    {
        // Implementation for updating configuration files
        // This would update appsettings.json and other config files
    }

    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var targetSubDir = Path.Combine(targetDir, dirName);
            await CopyDirectoryAsync(directory, targetSubDir);
        }
    }
}