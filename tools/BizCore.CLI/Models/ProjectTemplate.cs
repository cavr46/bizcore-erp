namespace BizCore.CLI.Models;

/// <summary>
/// Project template model
/// </summary>
public class ProjectTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string[] Features { get; set; }
    public bool IsDefault { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string[] Tags { get; set; }
    public string IconUrl { get; set; }
    public string DocumentationUrl { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Project information model
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string SolutionFile { get; set; }
    public string[] ProjectFiles { get; set; }
    public bool IsBizCoreProject { get; set; }
    public string Version { get; set; }
    public Dictionary<string, string> Configuration { get; set; } = new();
}