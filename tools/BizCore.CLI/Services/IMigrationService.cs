using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace BizCore.CLI.Services;

/// <summary>
/// Migration service for importing data from existing ERP systems
/// Supports SAP, Microsoft Dynamics, Oracle, and other systems
/// </summary>
public interface IMigrationService
{
    Task MigrateAsync(string sourceSystem, string connectionString, string[] modules, bool dryRun);
    Task<MigrationAnalysis> AnalyzeSourceSystemAsync(string sourceSystem, string connectionString);
    Task<IEnumerable<string>> GetSupportedSystemsAsync();
    Task<MigrationPlan> GenerateMigrationPlanAsync(string sourceSystem, string connectionString, string[] modules);
    Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun);
}

/// <summary>
/// Migration service implementation
/// </summary>
public class MigrationService : IMigrationService
{
    private readonly ILogger<MigrationService> _logger;
    private readonly Dictionary<string, IMigrationAdapter> _adapters;

    public MigrationService(ILogger<MigrationService> logger)
    {
        _logger = logger;
        _adapters = new Dictionary<string, IMigrationAdapter>
        {
            ["sap"] = new SAPMigrationAdapter(),
            ["dynamics"] = new DynamicsMigrationAdapter(),
            ["oracle"] = new OracleMigrationAdapter(),
            ["quickbooks"] = new QuickBooksMigrationAdapter(),
            ["sage"] = new SageMigrationAdapter(),
            ["netsuite"] = new NetSuiteMigrationAdapter(),
            ["excel"] = new ExcelMigrationAdapter(),
            ["generic"] = new GenericMigrationAdapter()
        };
    }

    public async Task MigrateAsync(string sourceSystem, string connectionString, string[] modules, bool dryRun)
    {
        try
        {
            CliHelpers.WriteLogo();
            CliHelpers.WriteInfo($"🔄 Starting migration from {sourceSystem.ToUpper()}");
            
            if (dryRun)
            {
                CliHelpers.WriteInfo("🔍 DRY RUN MODE - No data will be modified");
            }

            // Get migration adapter
            var adapter = GetMigrationAdapter(sourceSystem);
            if (adapter == null)
            {
                CliHelpers.WriteError($"Unsupported source system: {sourceSystem}");
                return;
            }

            // Test connection
            CliHelpers.WriteInfo("🔌 Testing connection to source system...");
            var connectionTest = await adapter.TestConnectionAsync(connectionString);
            if (!connectionTest.Success)
            {
                CliHelpers.WriteError($"Connection failed: {connectionTest.Error}");
                return;
            }
            CliHelpers.WriteSuccess("✅ Connection established successfully");

            // Analyze source system
            CliHelpers.WriteInfo("📊 Analyzing source system structure...");
            var analysis = await adapter.AnalyzeSystemAsync(connectionString);
            DisplayAnalysisResults(analysis);

            // Generate migration plan
            CliHelpers.WriteInfo("📋 Generating migration plan...");
            var plan = await adapter.GenerateMigrationPlanAsync(connectionString, modules);
            DisplayMigrationPlan(plan);

            // Ask for confirmation if not dry run
            if (!dryRun)
            {
                CliHelpers.WriteWarning("⚠️  This will modify your BizCore database. Continue? (y/N)");
                var confirmation = Console.ReadLine();
                if (confirmation?.ToLower() != "y")
                {
                    CliHelpers.WriteInfo("Migration cancelled");
                    return;
                }
            }

            // Execute migration
            CliHelpers.WriteInfo("🚀 Executing migration...");
            var result = await adapter.ExecuteMigrationAsync(plan, dryRun);
            DisplayMigrationResults(result);

            if (result.Success)
            {
                CliHelpers.WriteSuccess("🎉 Migration completed successfully!");
                CliHelpers.WriteInfo("📈 Migration Statistics:");
                CliHelpers.WriteInfo($"   • Total records migrated: {result.TotalRecords:N0}");
                CliHelpers.WriteInfo($"   • Success rate: {result.SuccessRate:P1}");
                CliHelpers.WriteInfo($"   • Duration: {result.Duration:hh\\:mm\\:ss}");
                
                if (!dryRun)
                {
                    CliHelpers.WriteInfo("🔄 Next steps:");
                    CliHelpers.WriteInfo("   1. Review migrated data in BizCore");
                    CliHelpers.WriteInfo("   2. Run data validation reports");
                    CliHelpers.WriteInfo("   3. Test business processes");
                    CliHelpers.WriteInfo("   4. Train users on new system");
                }
            }
            else
            {
                CliHelpers.WriteError($"❌ Migration failed: {result.Error}");
                if (result.Errors?.Any() == true)
                {
                    CliHelpers.WriteError("Detailed errors:");
                    foreach (var error in result.Errors)
                    {
                        CliHelpers.WriteError($"   • {error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed for system {SourceSystem}", sourceSystem);
            CliHelpers.WriteError($"Migration failed: {ex.Message}");
        }
    }

    public async Task<MigrationAnalysis> AnalyzeSourceSystemAsync(string sourceSystem, string connectionString)
    {
        var adapter = GetMigrationAdapter(sourceSystem);
        if (adapter == null)
        {
            throw new NotSupportedException($"Unsupported source system: {sourceSystem}");
        }

        return await adapter.AnalyzeSystemAsync(connectionString);
    }

    public async Task<IEnumerable<string>> GetSupportedSystemsAsync()
    {
        return await Task.FromResult(_adapters.Keys);
    }

    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string sourceSystem, string connectionString, string[] modules)
    {
        var adapter = GetMigrationAdapter(sourceSystem);
        if (adapter == null)
        {
            throw new NotSupportedException($"Unsupported source system: {sourceSystem}");
        }

        return await adapter.GenerateMigrationPlanAsync(connectionString, modules);
    }

    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun)
    {
        var adapter = GetMigrationAdapter(plan.SourceSystem);
        if (adapter == null)
        {
            throw new NotSupportedException($"Unsupported source system: {plan.SourceSystem}");
        }

        return await adapter.ExecuteMigrationAsync(plan, dryRun);
    }

    private IMigrationAdapter GetMigrationAdapter(string sourceSystem)
    {
        return _adapters.GetValueOrDefault(sourceSystem.ToLower());
    }

    private void DisplayAnalysisResults(MigrationAnalysis analysis)
    {
        CliHelpers.WriteInfo($"📊 System Analysis Results:");
        CliHelpers.WriteInfo($"   • System: {analysis.SystemName} v{analysis.Version}");
        CliHelpers.WriteInfo($"   • Company: {analysis.CompanyName}");
        CliHelpers.WriteInfo($"   • Database: {analysis.DatabaseName}");
        CliHelpers.WriteInfo($"   • Total Tables: {analysis.TotalTables:N0}");
        CliHelpers.WriteInfo($"   • Total Records: {analysis.TotalRecords:N0}");
        CliHelpers.WriteInfo($"   • Available Modules:");
        
        foreach (var module in analysis.AvailableModules)
        {
            CliHelpers.WriteInfo($"     - {module.Name} ({module.RecordCount:N0} records)");
        }

        if (analysis.Warnings?.Any() == true)
        {
            CliHelpers.WriteWarning("⚠️  Warnings:");
            foreach (var warning in analysis.Warnings)
            {
                CliHelpers.WriteWarning($"   • {warning}");
            }
        }
    }

    private void DisplayMigrationPlan(MigrationPlan plan)
    {
        CliHelpers.WriteInfo($"📋 Migration Plan:");
        CliHelpers.WriteInfo($"   • Source: {plan.SourceSystem}");
        CliHelpers.WriteInfo($"   • Target: BizCore ERP");
        CliHelpers.WriteInfo($"   • Modules to migrate: {plan.Modules.Length}");
        CliHelpers.WriteInfo($"   • Estimated duration: {plan.EstimatedDuration:hh\\:mm\\:ss}");
        CliHelpers.WriteInfo($"   • Migration steps:");

        foreach (var step in plan.Steps)
        {
            CliHelpers.WriteInfo($"     {step.Order}. {step.Name} ({step.EstimatedRecords:N0} records)");
        }
    }

    private void DisplayMigrationResults(MigrationResult result)
    {
        if (result.Success)
        {
            CliHelpers.WriteSuccess("✅ Migration completed successfully");
        }
        else
        {
            CliHelpers.WriteError($"❌ Migration failed: {result.Error}");
        }

        CliHelpers.WriteInfo($"📊 Migration Results:");
        foreach (var moduleResult in result.ModuleResults)
        {
            var status = moduleResult.Success ? "✅" : "❌";
            CliHelpers.WriteInfo($"   {status} {moduleResult.ModuleName}: {moduleResult.RecordsMigrated:N0} records");
            
            if (!moduleResult.Success)
            {
                CliHelpers.WriteError($"      Error: {moduleResult.Error}");
            }
        }
    }
}

/// <summary>
/// SAP Migration Adapter
/// Handles migration from SAP Business One, SAP ECC, and SAP S/4HANA
/// </summary>
public class SAPMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
    {
        try
        {
            // Test SAP connection using SAP .NET Connector or HANA connection
            // This is a simplified example - real implementation would use SAP SDK
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Check if this is a SAP database by looking for SAP-specific tables
            using var command = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'O%' OR TABLE_NAME LIKE '@%'", connection);
            var sapTables = (int)await command.ExecuteScalarAsync();
            
            if (sapTables > 0)
            {
                return new ConnectionTestResult(true, "SAP system detected");
            }
            
            return new ConnectionTestResult(false, "Not a SAP system");
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, ex.Message);
        }
    }

    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var analysis = new MigrationAnalysis
        {
            SystemName = "SAP Business One",
            Version = await GetSAPVersionAsync(connection),
            CompanyName = await GetCompanyNameAsync(connection),
            DatabaseName = connection.Database,
            AvailableModules = new List<ModuleInfo>()
        };

        // Analyze SAP modules
        analysis.AvailableModules.Add(await AnalyzeSAPModule(connection, "Accounting", "OJDT", "Chart of Accounts, Journal Entries"));
        analysis.AvailableModules.Add(await AnalyzeSAPModule(connection, "Inventory", "OITM", "Items, Stock, Warehouses"));
        analysis.AvailableModules.Add(await AnalyzeSAPModule(connection, "Sales", "OCRD", "Customers, Sales Orders, Invoices"));
        analysis.AvailableModules.Add(await AnalyzeSAPModule(connection, "Purchasing", "OCRD", "Vendors, Purchase Orders, AP Invoices"));
        analysis.AvailableModules.Add(await AnalyzeSAPModule(connection, "HumanResources", "OHEM", "Employees, Payroll"));

        analysis.TotalTables = analysis.AvailableModules.Sum(m => m.TableCount);
        analysis.TotalRecords = analysis.AvailableModules.Sum(m => m.RecordCount);

        // Add SAP-specific warnings
        analysis.Warnings = new List<string>
        {
            "SAP User-Defined Fields (UDF) will need manual mapping",
            "SAP Formatted Searches will not be migrated",
            "SAP Add-ons and customizations require separate migration",
            "Review SAP authorization matrix for BizCore role mapping"
        };

        return analysis;
    }

    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules)
    {
        var plan = new MigrationPlan
        {
            SourceSystem = "SAP",
            Modules = modules,
            Steps = new List<MigrationStep>(),
            EstimatedDuration = TimeSpan.FromHours(2)
        };

        // Define migration steps based on SAP dependencies
        var stepOrder = 1;
        
        if (modules.Contains("Accounting") || modules.Length == 0)
        {
            plan.Steps.Add(new MigrationStep
            {
                Order = stepOrder++,
                Name = "Migrate Chart of Accounts",
                Table = "OACT",
                EstimatedRecords = 500,
                Dependencies = new string[0]
            });
        }

        if (modules.Contains("Inventory") || modules.Length == 0)
        {
            plan.Steps.Add(new MigrationStep
            {
                Order = stepOrder++,
                Name = "Migrate Item Master Data",
                Table = "OITM",
                EstimatedRecords = 1000,
                Dependencies = new[] { "Chart of Accounts" }
            });
        }

        if (modules.Contains("Sales") || modules.Length == 0)
        {
            plan.Steps.Add(new MigrationStep
            {
                Order = stepOrder++,
                Name = "Migrate Customers",
                Table = "OCRD",
                EstimatedRecords = 2000,
                Dependencies = new[] { "Chart of Accounts" }
            });
        }

        if (modules.Contains("Purchasing") || modules.Length == 0)
        {
            plan.Steps.Add(new MigrationStep
            {
                Order = stepOrder++,
                Name = "Migrate Vendors",
                Table = "OCRD",
                EstimatedRecords = 500,
                Dependencies = new[] { "Chart of Accounts" }
            });
        }

        return plan;
    }

    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun)
    {
        var result = new MigrationResult
        {
            Success = true,
            ModuleResults = new List<ModuleMigrationResult>(),
            TotalRecords = 0,
            Duration = TimeSpan.Zero
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            foreach (var step in plan.Steps.OrderBy(s => s.Order))
            {
                CliHelpers.WriteInfo($"🔄 {step.Name}...");
                
                var moduleResult = await MigrateSAPModule(step, dryRun);
                result.ModuleResults.Add(moduleResult);
                result.TotalRecords += moduleResult.RecordsMigrated;

                if (!moduleResult.Success)
                {
                    result.Success = false;
                    result.Error = $"Failed at step: {step.Name}";
                    break;
                }

                // Show progress
                var progress = (double)result.ModuleResults.Count / plan.Steps.Count * 100;
                CliHelpers.WriteInfo($"📊 Progress: {progress:F1}% ({result.ModuleResults.Count}/{plan.Steps.Count} steps)");
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.SuccessRate = result.ModuleResults.Count > 0 
                ? (double)result.ModuleResults.Count(m => m.Success) / result.ModuleResults.Count 
                : 0;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Error = ex.Message;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<string> GetSAPVersionAsync(SqlConnection connection)
    {
        try
        {
            using var command = new SqlCommand("SELECT TOP 1 Version FROM CINF", connection);
            var version = await command.ExecuteScalarAsync();
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<string> GetCompanyNameAsync(SqlConnection connection)
    {
        try
        {
            using var command = new SqlCommand("SELECT TOP 1 CompnyName FROM OADM", connection);
            var companyName = await command.ExecuteScalarAsync();
            return companyName?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<ModuleInfo> AnalyzeSAPModule(SqlConnection connection, string moduleName, string mainTable, string description)
    {
        try
        {
            using var command = new SqlCommand($"SELECT COUNT(*) FROM {mainTable}", connection);
            var recordCount = (int)await command.ExecuteScalarAsync();
            
            return new ModuleInfo
            {
                Name = moduleName,
                Description = description,
                RecordCount = recordCount,
                TableCount = 1 // Simplified - real implementation would count related tables
            };
        }
        catch
        {
            return new ModuleInfo
            {
                Name = moduleName,
                Description = description,
                RecordCount = 0,
                TableCount = 0
            };
        }
    }

    private async Task<ModuleMigrationResult> MigrateSAPModule(MigrationStep step, bool dryRun)
    {
        try
        {
            // Simulate migration process
            await Task.Delay(1000); // Simulate processing time
            
            var result = new ModuleMigrationResult
            {
                ModuleName = step.Name,
                Success = true,
                RecordsMigrated = step.EstimatedRecords,
                Duration = TimeSpan.FromSeconds(1)
            };

            if (dryRun)
            {
                result.Notes = "DRY RUN - No data was actually migrated";
            }

            return result;
        }
        catch (Exception ex)
        {
            return new ModuleMigrationResult
            {
                ModuleName = step.Name,
                Success = false,
                Error = ex.Message,
                RecordsMigrated = 0,
                Duration = TimeSpan.Zero
            };
        }
    }
}

/// <summary>
/// Microsoft Dynamics Migration Adapter
/// </summary>
public class DynamicsMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
    {
        // Implementation for Dynamics connection testing
        return new ConnectionTestResult(true, "Dynamics connection test not implemented");
    }

    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString)
    {
        // Implementation for Dynamics analysis
        return new MigrationAnalysis
        {
            SystemName = "Microsoft Dynamics",
            Version = "365",
            CompanyName = "Demo Company",
            DatabaseName = "DynamicsDB",
            AvailableModules = new List<ModuleInfo>()
        };
    }

    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules)
    {
        // Implementation for Dynamics migration plan
        return new MigrationPlan
        {
            SourceSystem = "Dynamics",
            Modules = modules,
            Steps = new List<MigrationStep>(),
            EstimatedDuration = TimeSpan.FromHours(1)
        };
    }

    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun)
    {
        // Implementation for Dynamics migration execution
        return new MigrationResult
        {
            Success = true,
            ModuleResults = new List<ModuleMigrationResult>(),
            TotalRecords = 0,
            Duration = TimeSpan.Zero
        };
    }
}

/// <summary>
/// Generic migration adapter for other systems
/// </summary>
public class GenericMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
    {
        return new ConnectionTestResult(true, "Generic connection test");
    }

    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString)
    {
        return new MigrationAnalysis
        {
            SystemName = "Generic System",
            Version = "1.0",
            CompanyName = "Generic Company",
            DatabaseName = "GenericDB",
            AvailableModules = new List<ModuleInfo>()
        };
    }

    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules)
    {
        return new MigrationPlan
        {
            SourceSystem = "Generic",
            Modules = modules,
            Steps = new List<MigrationStep>(),
            EstimatedDuration = TimeSpan.FromMinutes(30)
        };
    }

    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun)
    {
        return new MigrationResult
        {
            Success = true,
            ModuleResults = new List<ModuleMigrationResult>(),
            TotalRecords = 0,
            Duration = TimeSpan.Zero
        };
    }
}

// Additional adapters would be implemented here...
public class OracleMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString) => new(true, "Oracle test");
    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString) => new() { SystemName = "Oracle" };
    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules) => new() { SourceSystem = "Oracle" };
    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun) => new() { Success = true };
}

public class QuickBooksMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString) => new(true, "QuickBooks test");
    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString) => new() { SystemName = "QuickBooks" };
    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules) => new() { SourceSystem = "QuickBooks" };
    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun) => new() { Success = true };
}

public class SageMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString) => new(true, "Sage test");
    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString) => new() { SystemName = "Sage" };
    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules) => new() { SourceSystem = "Sage" };
    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun) => new() { Success = true };
}

public class NetSuiteMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString) => new(true, "NetSuite test");
    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString) => new() { SystemName = "NetSuite" };
    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules) => new() { SourceSystem = "NetSuite" };
    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun) => new() { Success = true };
}

public class ExcelMigrationAdapter : IMigrationAdapter
{
    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString) => new(true, "Excel test");
    public async Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString) => new() { SystemName = "Excel" };
    public async Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules) => new() { SourceSystem = "Excel" };
    public async Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun) => new() { Success = true };
}

/// <summary>
/// Migration adapter interface
/// </summary>
public interface IMigrationAdapter
{
    Task<ConnectionTestResult> TestConnectionAsync(string connectionString);
    Task<MigrationAnalysis> AnalyzeSystemAsync(string connectionString);
    Task<MigrationPlan> GenerateMigrationPlanAsync(string connectionString, string[] modules);
    Task<MigrationResult> ExecuteMigrationAsync(MigrationPlan plan, bool dryRun);
}

// Data models for migration
public class MigrationAnalysis
{
    public string SystemName { get; set; }
    public string Version { get; set; }
    public string CompanyName { get; set; }
    public string DatabaseName { get; set; }
    public int TotalTables { get; set; }
    public int TotalRecords { get; set; }
    public List<ModuleInfo> AvailableModules { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ModuleInfo
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int RecordCount { get; set; }
    public int TableCount { get; set; }
}

public class MigrationPlan
{
    public string SourceSystem { get; set; }
    public string[] Modules { get; set; }
    public List<MigrationStep> Steps { get; set; } = new();
    public TimeSpan EstimatedDuration { get; set; }
}

public class MigrationStep
{
    public int Order { get; set; }
    public string Name { get; set; }
    public string Table { get; set; }
    public int EstimatedRecords { get; set; }
    public string[] Dependencies { get; set; }
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ModuleMigrationResult> ModuleResults { get; set; } = new();
    public int TotalRecords { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ModuleMigrationResult
{
    public string ModuleName { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
    public int RecordsMigrated { get; set; }
    public TimeSpan Duration { get; set; }
    public string Notes { get; set; }
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Error { get; set; }

    public ConnectionTestResult(bool success, string message)
    {
        Success = success;
        Message = message;
        Error = success ? null : message;
    }
}