using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BizCore.Localization;

/// <summary>
/// Service collection extensions for BizCore localization
/// </summary>
public static class LocalizationServiceExtensions
{
    /// <summary>
    /// Add BizCore localization services
    /// </summary>
    public static IServiceCollection AddBizCoreLocalization(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure localization options
        services.Configure<LocalizationConfiguration>(configuration.GetSection("Localization"));
        
        // Add memory cache for translations
        services.AddMemoryCache();
        
        // Add localization services
        services.AddScoped<ILocalizationService, LocalizationService>();
        
        // Add repository based on environment
        var environment = services.BuildServiceProvider().GetRequiredService<IHostEnvironment>();
        if (environment.IsDevelopment())
        {
            services.AddScoped<ILocalizationRepository, JsonLocalizationRepository>();
        }
        else
        {
            services.AddScoped<ILocalizationRepository, SqlLocalizationRepository>();
        }
        
        // Add HTTP context accessor for culture detection
        services.AddHttpContextAccessor();
        
        return services;
    }

    /// <summary>
    /// Use BizCore localization middleware
    /// </summary>
    public static IApplicationBuilder UseBizCoreLocalization(this IApplicationBuilder app)
    {
        // Add localization middleware
        app.UseMiddleware<LocalizationMiddleware>();
        
        // Configure request localization
        var supportedCultures = app.ApplicationServices
            .GetRequiredService<IOptions<LocalizationConfiguration>>()
            .Value.SupportedCultures;
        
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        
        app.UseRequestLocalization(localizationOptions);
        
        return app;
    }

    /// <summary>
    /// Initialize localization database
    /// </summary>
    public static async Task InitializeBizCoreLocalizationAsync(this IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            await LocalizationDatabaseMigration.EnsureTablesExistAsync(connectionString);
            await LocalizationDatabaseMigration.SeedDefaultTranslationsAsync(connectionString);
        }
    }
}

/// <summary>
/// Localization middleware for automatic culture detection
/// </summary>
public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalizationMiddleware> _logger;

    public LocalizationMiddleware(RequestDelegate next, ILogger<LocalizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var localizationService = context.RequestServices.GetRequiredService<ILocalizationService>();
            var configuration = context.RequestServices.GetRequiredService<IOptions<LocalizationConfiguration>>();
            
            // Check if culture is specified in query parameter
            var cultureFromQuery = context.Request.Query["culture"].FirstOrDefault();
            
            // Check if culture is specified in header
            var cultureFromHeader = context.Request.Headers["Accept-Language"].FirstOrDefault();
            
            // Check if culture is stored in cookie
            var cultureFromCookie = context.Request.Cookies["selectedCulture"];
            
            // Check if culture is stored in user claims
            var cultureFromClaims = context.User.Claims
                .FirstOrDefault(c => c.Type == "preferred_language")?.Value;

            // Determine culture priority: query > cookie > claims > header > default
            var selectedCulture = cultureFromQuery ?? 
                                cultureFromCookie ?? 
                                cultureFromClaims ?? 
                                GetBestMatchingCulture(cultureFromHeader, configuration.Value.SupportedCultures) ?? 
                                configuration.Value.DefaultCulture;

            // Set culture if different from current
            var currentCulture = localizationService.GetCurrentCulture().Name;
            if (selectedCulture != currentCulture && 
                configuration.Value.SupportedCultures.Contains(selectedCulture))
            {
                await localizationService.SetCultureAsync(selectedCulture);
                
                // Update cookie if culture came from query or header
                if (cultureFromQuery != null || (cultureFromHeader != null && cultureFromCookie == null))
                {
                    context.Response.Cookies.Append("selectedCulture", selectedCulture, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    });
                }
            }

            // Add culture information to response headers
            context.Response.Headers.Add("Content-Language", localizationService.GetCurrentCulture().Name);
            context.Response.Headers.Add("X-Culture", localizationService.GetCurrentCulture().Name);
            context.Response.Headers.Add("X-RTL", localizationService.IsRightToLeft.ToString().ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in localization middleware");
            // Continue processing even if localization fails
        }

        await _next(context);
    }

    private string GetBestMatchingCulture(string acceptLanguageHeader, string[] supportedCultures)
    {
        if (string.IsNullOrEmpty(acceptLanguageHeader))
            return null;

        var acceptedLanguages = acceptLanguageHeader
            .Split(',')
            .Select(lang => lang.Trim().Split(';')[0])
            .ToList();

        // Try exact match first
        foreach (var acceptedLang in acceptedLanguages)
        {
            if (supportedCultures.Contains(acceptedLang))
                return acceptedLang;
        }

        // Try partial match (language without country)
        foreach (var acceptedLang in acceptedLanguages)
        {
            var langOnly = acceptedLang.Split('-')[0];
            var match = supportedCultures.FirstOrDefault(sc => sc.StartsWith(langOnly + "-"));
            if (match != null)
                return match;
        }

        return null;
    }
}

/// <summary>
/// Localization helper extensions for commonly used patterns
/// </summary>
public static class LocalizationHelpers
{
    /// <summary>
    /// Get localized validation message
    /// </summary>
    public static string GetValidationMessage(this ILocalizationService localization, string validationType, params object[] args)
    {
        var key = $"Validation.{validationType}";
        return localization.GetString(key, key, args);
    }

    /// <summary>
    /// Get localized error message
    /// </summary>
    public static string GetErrorMessage(this ILocalizationService localization, string errorType, params object[] args)
    {
        var key = $"Error.{errorType}";
        return localization.GetString(key, key, args);
    }

    /// <summary>
    /// Get localized success message
    /// </summary>
    public static string GetSuccessMessage(this ILocalizationService localization, string successType, params object[] args)
    {
        var key = $"Success.{successType}";
        return localization.GetString(key, key, args);
    }

    /// <summary>
    /// Get localized navigation item
    /// </summary>
    public static string GetNavigationItem(this ILocalizationService localization, string navigationItem)
    {
        var key = $"Navigation.{navigationItem}";
        return localization.GetString(key, navigationItem);
    }

    /// <summary>
    /// Get localized common action
    /// </summary>
    public static string GetCommonAction(this ILocalizationService localization, string action)
    {
        var key = $"Common.{action}";
        return localization.GetString(key, action);
    }

    /// <summary>
    /// Get localized field label
    /// </summary>
    public static string GetFieldLabel(this ILocalizationService localization, string module, string field)
    {
        var key = $"{module}.{field}";
        return localization.GetString(key, field);
    }

    /// <summary>
    /// Format localized message with pluralization
    /// </summary>
    public static string GetPluralizedMessage(this ILocalizationService localization, string key, int count, params object[] args)
    {
        var pluralKey = count == 1 ? $"{key}.Singular" : $"{key}.Plural";
        var message = localization.GetString(pluralKey, key, args);
        return string.Format(message, count);
    }

    /// <summary>
    /// Get localized enum value
    /// </summary>
    public static string GetEnumText<T>(this ILocalizationService localization, T enumValue) where T : Enum
    {
        var key = $"Enum.{typeof(T).Name}.{enumValue}";
        return localization.GetString(key, enumValue.ToString());
    }

    /// <summary>
    /// Get localized date format pattern
    /// </summary>
    public static string GetDateFormatPattern(this ILocalizationService localization, string patternType = "Short")
    {
        var key = $"DateFormat.{patternType}";
        var culture = localization.GetCurrentCulture();
        
        return patternType.ToLower() switch
        {
            "short" => culture.DateTimeFormat.ShortDatePattern,
            "long" => culture.DateTimeFormat.LongDatePattern,
            "time" => culture.DateTimeFormat.ShortTimePattern,
            "datetime" => culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern,
            _ => localization.GetString(key, culture.DateTimeFormat.ShortDatePattern)
        };
    }

    /// <summary>
    /// Get localized number format
    /// </summary>
    public static string GetNumberFormat(this ILocalizationService localization, string formatType = "Decimal")
    {
        var culture = localization.GetCurrentCulture();
        
        return formatType.ToLower() switch
        {
            "decimal" => "N2",
            "currency" => "C",
            "percentage" => "P",
            "integer" => "N0",
            _ => "N2"
        };
    }
}

/// <summary>
/// String extensions for localization
/// </summary>
public static class StringLocalizationExtensions
{
    /// <summary>
    /// Check if string is null or empty
    /// </summary>
    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Check if string is null or whitespace
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Truncate string with ellipsis
    /// </summary>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Convert string to title case respecting culture
    /// </summary>
    public static string ToTitleCase(this string value, CultureInfo culture = null)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        culture ??= CultureInfo.CurrentCulture;
        return culture.TextInfo.ToTitleCase(value.ToLower());
    }
}