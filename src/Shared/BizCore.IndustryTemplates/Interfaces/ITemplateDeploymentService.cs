using BizCore.IndustryTemplates.Models;

namespace BizCore.IndustryTemplates.Interfaces;

/// <summary>
/// Template deployment service interface
/// </summary>
public interface ITemplateDeploymentService
{
    /// <summary>
    /// Deploy template to tenant
    /// </summary>
    Task<IndustryTemplateResult> DeployTemplateAsync(string templateId, string tenantId, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Get deployment status
    /// </summary>
    Task<Dictionary<string, object>> GetDeploymentStatusAsync(string deploymentId);

    /// <summary>
    /// Cancel deployment
    /// </summary>
    Task<bool> CancelDeploymentAsync(string deploymentId);

    /// <summary>
    /// Rollback deployment
    /// </summary>
    Task<IndustryTemplateResult> RollbackDeploymentAsync(string deploymentId);

    /// <summary>
    /// Get deployment history
    /// </summary>
    Task<IEnumerable<Dictionary<string, object>>> GetDeploymentHistoryAsync(string tenantId, int skip = 0, int take = 50);

    /// <summary>
    /// Validate deployment prerequisites
    /// </summary>
    Task<IndustryTemplateResult> ValidateDeploymentPrerequisitesAsync(string templateId, string tenantId);

    /// <summary>
    /// Preview deployment changes
    /// </summary>
    Task<Dictionary<string, object>> PreviewDeploymentAsync(string templateId, string tenantId, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Schedule deployment
    /// </summary>
    Task<IndustryTemplateResult> ScheduleDeploymentAsync(string templateId, string tenantId, DateTime scheduledTime, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Update deployment
    /// </summary>
    Task<IndustryTemplateResult> UpdateDeploymentAsync(string deploymentId, string newTemplateVersion);

    /// <summary>
    /// Get deployed templates for tenant
    /// </summary>
    Task<IEnumerable<IndustryTemplate>> GetDeployedTemplatesAsync(string tenantId);
}