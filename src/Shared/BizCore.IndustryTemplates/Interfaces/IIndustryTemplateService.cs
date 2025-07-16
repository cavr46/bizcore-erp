using BizCore.IndustryTemplates.Models;

namespace BizCore.IndustryTemplates.Interfaces;

/// <summary>
/// Core industry template service interface
/// </summary>
public interface IIndustryTemplateService
{
    /// <summary>
    /// Create industry template
    /// </summary>
    Task<IndustryTemplateResult> CreateTemplateAsync(CreateIndustryTemplateRequest request);

    /// <summary>
    /// Update industry template
    /// </summary>
    Task<IndustryTemplateResult> UpdateTemplateAsync(string templateId, IndustryTemplate template);

    /// <summary>
    /// Get industry template by ID
    /// </summary>
    Task<IndustryTemplate?> GetTemplateAsync(string templateId);

    /// <summary>
    /// Get templates by industry
    /// </summary>
    Task<IEnumerable<IndustryTemplate>> GetTemplatesByIndustryAsync(IndustryType industry, TemplateSize? size = null, TemplateComplexity? complexity = null);

    /// <summary>
    /// Search templates
    /// </summary>
    Task<IEnumerable<IndustryTemplate>> SearchTemplatesAsync(string searchTerm, List<string>? tags = null, string? category = null, int skip = 0, int take = 20);

    /// <summary>
    /// Get popular templates
    /// </summary>
    Task<IEnumerable<IndustryTemplate>> GetPopularTemplatesAsync(int count = 10);

    /// <summary>
    /// Get recommended templates for tenant
    /// </summary>
    Task<IEnumerable<IndustryTemplate>> GetRecommendedTemplatesAsync(string tenantId, IndustryType? industry = null);

    /// <summary>
    /// Delete template
    /// </summary>
    Task<bool> DeleteTemplateAsync(string templateId);

    /// <summary>
    /// Publish template
    /// </summary>
    Task<IndustryTemplateResult> PublishTemplateAsync(string templateId);

    /// <summary>
    /// Validate template
    /// </summary>
    Task<IndustryTemplateResult> ValidateTemplateAsync(string templateId);

    /// <summary>
    /// Clone template
    /// </summary>
    Task<IndustryTemplateResult> CloneTemplateAsync(string sourceTemplateId, string newName, string? newDescription = null);

    /// <summary>
    /// Export template
    /// </summary>
    Task<byte[]> ExportTemplateAsync(string templateId, string format = "json");

    /// <summary>
    /// Import template
    /// </summary>
    Task<IndustryTemplateResult> ImportTemplateAsync(byte[] templateData, string format = "json");
}