using BizCore.Monitoring.Models;

namespace BizCore.Monitoring.Interfaces;

/// <summary>
/// Service Level Objectives (SLO) management service interface
/// </summary>
public interface ISLOService
{
    /// <summary>
    /// Create SLO
    /// </summary>
    Task<MonitoringResult> CreateSLOAsync(ServiceLevelObjective slo);

    /// <summary>
    /// Update SLO
    /// </summary>
    Task<MonitoringResult> UpdateSLOAsync(string sloId, ServiceLevelObjective slo);

    /// <summary>
    /// Get SLO by ID
    /// </summary>
    Task<ServiceLevelObjective?> GetSLOAsync(string sloId);

    /// <summary>
    /// Get SLOs for tenant
    /// </summary>
    Task<IEnumerable<ServiceLevelObjective>> GetSLOsAsync(string tenantId, string? serviceName = null);

    /// <summary>
    /// Delete SLO
    /// </summary>
    Task<bool> DeleteSLOAsync(string sloId);

    /// <summary>
    /// Calculate SLO status
    /// </summary>
    Task<SLOStatus> CalculateSLOStatusAsync(string sloId, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Get SLO compliance report
    /// </summary>
    Task<Dictionary<string, object>> GetSLOComplianceReportAsync(string sloId, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Get error budget burn rate
    /// </summary>
    Task<double> GetErrorBudgetBurnRateAsync(string sloId, TimeSpan window);

    /// <summary>
    /// Get SLO violations
    /// </summary>
    Task<IEnumerable<SLOViolation>> GetSLOViolationsAsync(string sloId, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Predict SLO breach
    /// </summary>
    Task<Dictionary<string, object>> PredictSLOBreachAsync(string sloId, TimeSpan predictionWindow);

    /// <summary>
    /// Get SLO dashboard data
    /// </summary>
    Task<Dictionary<string, object>> GetSLODashboardDataAsync(string sloId, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Bulk calculate SLO status
    /// </summary>
    Task<Dictionary<string, SLOStatus>> BulkCalculateSLOStatusAsync(List<string> sloIds, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// Get SLO trends
    /// </summary>
    Task<Dictionary<string, object>> GetSLOTrendsAsync(string sloId, TimeSpan period, string granularity = "daily");
}