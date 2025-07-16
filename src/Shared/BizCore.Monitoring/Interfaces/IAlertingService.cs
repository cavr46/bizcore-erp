using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Alerting and notification service interface
/// </summary>
public interface IAlertingService
{
    /// <summary>
    /// Create alert rule
    /// </summary>
    Task<MonitoringResult> CreateAlertRuleAsync(CreateAlertRuleRequest request);

    /// <summary>
    /// Update alert rule
    /// </summary>
    Task<MonitoringResult> UpdateAlertRuleAsync(string ruleId, AlertRule rule);

    /// <summary>
    /// Get alert rule by ID
    /// </summary>
    Task<AlertRule?> GetAlertRuleAsync(string ruleId);

    /// <summary>
    /// Get alert rules for tenant
    /// </summary>
    Task<IEnumerable<AlertRule>> GetAlertRulesAsync(string tenantId, bool enabledOnly = false);

    /// <summary>
    /// Delete alert rule
    /// </summary>
    Task<bool> DeleteAlertRuleAsync(string ruleId);

    /// <summary>
    /// Enable/disable alert rule
    /// </summary>
    Task<bool> SetAlertRuleStatusAsync(string ruleId, bool isEnabled);

    /// <summary>
    /// Test alert rule
    /// </summary>
    Task<MonitoringResult> TestAlertRuleAsync(string ruleId);

    /// <summary>
    /// Trigger alert manually
    /// </summary>
    Task<MonitoringResult> TriggerAlertAsync(string ruleId, Dictionary<string, object>? context = null);

    /// <summary>
    /// Get active alerts
    /// </summary>
    Task<IEnumerable<Alert>> GetActiveAlertsAsync(string tenantId);

    /// <summary>
    /// Get alert by ID
    /// </summary>
    Task<Alert?> GetAlertAsync(string alertId);

    /// <summary>
    /// Get alert history
    /// </summary>
    Task<IEnumerable<Alert>> GetAlertHistoryAsync(string tenantId, DateTime? startTime = null, DateTime? endTime = null, int skip = 0, int take = 100);

    /// <summary>
    /// Acknowledge alert
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy);

    /// <summary>
    /// Resolve alert
    /// </summary>
    Task<bool> ResolveAlertAsync(string alertId, string resolvedBy);

    /// <summary>
    /// Suppress alert
    /// </summary>
    Task<bool> SuppressAlertAsync(string alertId, TimeSpan duration, string suppressedBy);

    /// <summary>
    /// Get alert statistics
    /// </summary>
    Task<Dictionary<string, object>> GetAlertStatisticsAsync(string tenantId, DateTime startTime, DateTime endTime);
}