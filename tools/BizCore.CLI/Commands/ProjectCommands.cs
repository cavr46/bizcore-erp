using System.CommandLine;
using BizCore.CLI.Services;

namespace BizCore.CLI.Commands;

/// <summary>
/// Project management commands
/// </summary>
public static class ProjectCommands
{
    public static Command CreateProjectCommand(IServiceProvider services)
    {
        var command = new Command("project", "Project management commands");
        
        // Create project command
        var createCommand = new Command("create", "Create new BizCore project");
        
        var nameOption = new Option<string>("--name", "Project name") { IsRequired = true };
        var templateOption = new Option<string>("--template", "Project template");
        var outputOption = new Option<string>("--output", "Output directory");
        var forceOption = new Option<bool>("--force", "Force creation even if directory exists");
        
        createCommand.AddOption(nameOption);
        createCommand.AddOption(templateOption);
        createCommand.AddOption(outputOption);
        createCommand.AddOption(forceOption);

        createCommand.SetHandler(async (name, template, output, force) =>
        {
            var projectService = services.GetRequiredService<IProjectService>();
            await projectService.CreateProjectAsync(name, template, output, force);
        }, nameOption, templateOption, outputOption, forceOption);

        // Init command
        var initCommand = new Command("init", "Initialize existing project as BizCore project");
        var pathOption = new Option<string>("--path", "Project path");
        
        initCommand.AddOption(pathOption);
        initCommand.SetHandler(async (path) =>
        {
            var projectService = services.GetRequiredService<IProjectService>();
            await projectService.InitializeProjectAsync(path ?? Directory.GetCurrentDirectory());
        }, pathOption);

        // Update command
        var updateCommand = new Command("update", "Update project to latest version");
        var versionOption = new Option<string>("--version", "Target version");
        
        updateCommand.AddOption(pathOption);
        updateCommand.AddOption(versionOption);
        updateCommand.SetHandler(async (path, version) =>
        {
            var projectService = services.GetRequiredService<IProjectService>();
            await projectService.UpdateProjectAsync(path ?? Directory.GetCurrentDirectory(), version ?? "latest");
        }, pathOption, versionOption);

        // Info command
        var infoCommand = new Command("info", "Show project information");
        infoCommand.AddOption(pathOption);
        infoCommand.SetHandler(async (path) =>
        {
            var projectService = services.GetRequiredService<IProjectService>();
            var info = await projectService.GetProjectInfoAsync(path ?? Directory.GetCurrentDirectory());
            
            if (info != null)
            {
                CliHelpers.WriteInfo($"Project: {info.Name}");
                CliHelpers.WriteInfo($"Path: {info.Path}");
                CliHelpers.WriteInfo($"Version: {info.Version}");
                CliHelpers.WriteInfo($"BizCore Project: {info.IsBizCoreProject}");
                CliHelpers.WriteInfo($"Projects: {info.ProjectFiles.Length}");
            }
            else
            {
                CliHelpers.WriteWarning("Not a valid project directory");
            }
        }, pathOption);

        command.AddCommand(createCommand);
        command.AddCommand(initCommand);
        command.AddCommand(updateCommand);
        command.AddCommand(infoCommand);

        return command;
    }
}