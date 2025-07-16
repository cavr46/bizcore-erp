using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Dashboard and visualization service interface
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Create dashboard
    /// </summary>
    Task<MonitoringResult> CreateDashboardAsync(CreateDashboardRequest request);

    /// <summary>
    /// Update dashboard
    /// </summary>
    Task<MonitoringResult> UpdateDashboardAsync(string dashboardId, Dashboard dashboard);

    /// <summary>
    /// Get dashboard by ID
    /// </summary>
    Task<Dashboard?> GetDashboardAsync(string dashboardId);

    /// <summary>
    /// Get dashboards for tenant
    /// </summary>
    Task<IEnumerable<Dashboard>> GetDashboardsAsync(string tenantId, bool publicOnly = false);

    /// <summary>
    /// Delete dashboard
    /// </summary>
    Task<bool> DeleteDashboardAsync(string dashboardId);

    /// <summary>
    /// Clone dashboard
    /// </summary>
    Task<MonitoringResult> CloneDashboardAsync(string sourceDashboardId, string newName, string tenantId);

    /// <summary>
    /// Add widget to dashboard
    /// </summary>
    Task<MonitoringResult> AddWidgetAsync(string dashboardId, DashboardWidget widget);

    /// <summary>
    /// Update widget
    /// </summary>
    Task<MonitoringResult> UpdateWidgetAsync(string dashboardId, string widgetId, DashboardWidget widget);

    /// <summary>
    /// Remove widget from dashboard
    /// </summary>
    Task<bool> RemoveWidgetAsync(string dashboardId, string widgetId);

    /// <summary>
    /// Get widget data
    /// </summary>
    Task<QueryResult> GetWidgetDataAsync(string dashboardId, string widgetId, Dictionary<string, object>? variables = null);

    /// <summary>
    /// Refresh widget data
    /// </summary>
    Task<QueryResult> RefreshWidgetDataAsync(string dashboardId, string widgetId);

    /// <summary>
    /// Export dashboard
    /// </summary>
    Task<MonitoringResult> ExportDashboardAsync(string dashboardId, string format = "json");

    /// <summary>
    /// Import dashboard
    /// </summary>
    Task<MonitoringResult> ImportDashboardAsync(string content, string format, string tenantId);

    /// <summary>
    /// Get dashboard templates
    /// </summary>
    Task<IEnumerable<Dashboard>> GetDashboardTemplatesAsync();

    /// <summary>
    /// Create dashboard from template
    /// </summary>
    Task<MonitoringResult> CreateFromTemplateAsync(string templateId, string name, string tenantId, Dictionary<string, object>? variables = null);

    /// <summary>
    /// Share dashboard
    /// </summary>
    Task<MonitoringResult> ShareDashboardAsync(string dashboardId, List<string> userIds, bool isPublic = false);

    /// <summary>
    /// Get shared dashboards for user
    /// </summary>
    Task<IEnumerable<Dashboard>> GetSharedDashboardsAsync(string userId);
}