using System.Globalization;

namespace BizCore.Localization;

/// <summary>
/// Localization service interface for BizCore ERP
/// Provides advanced multi-language support with real-time switching
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Get localized string by key
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Get localized string by key with fallback
    /// </summary>
    string GetString(string key, string fallback, params object[] args);

    /// <summary>
    /// Get localized string for specific culture
    /// </summary>
    string GetString(string key, CultureInfo culture, params object[] args);

    /// <summary>
    /// Get all translations for a key across all cultures
    /// </summary>
    Dictionary<string, string> GetAllTranslations(string key);

    /// <summary>
    /// Set current culture
    /// </summary>
    Task SetCultureAsync(string cultureName);

    /// <summary>
    /// Get current culture
    /// </summary>
    CultureInfo GetCurrentCulture();

    /// <summary>
    /// Get supported cultures
    /// </summary>
    IEnumerable<CultureInfo> GetSupportedCultures();

    /// <summary>
    /// Check if key exists in current culture
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Get completion percentage for a culture
    /// </summary>
    double GetCompletionPercentage(string cultureName);

    /// <summary>
    /// Get missing translations for a culture
    /// </summary>
    IEnumerable<string> GetMissingTranslations(string cultureName);

    /// <summary>
    /// Add or update translation
    /// </summary>
    Task AddOrUpdateTranslationAsync(string key, string cultureName, string value);

    /// <summary>
    /// Remove translation
    /// </summary>
    Task RemoveTranslationAsync(string key, string cultureName);

    /// <summary>
    /// Import translations from file
    /// </summary>
    Task ImportTranslationsAsync(string cultureName, Stream fileStream, string format = "json");

    /// <summary>
    /// Export translations to file
    /// </summary>
    Task<Stream> ExportTranslationsAsync(string cultureName, string format = "json");

    /// <summary>
    /// Get localized currency format
    /// </summary>
    string FormatCurrency(decimal amount, string currencyCode = null);

    /// <summary>
    /// Get localized date format
    /// </summary>
    string FormatDate(DateTime date, string format = null);

    /// <summary>
    /// Get localized number format
    /// </summary>
    string FormatNumber(decimal number, int decimals = 2);

    /// <summary>
    /// Get localized percentage format
    /// </summary>
    string FormatPercentage(double percentage, int decimals = 2);

    /// <summary>
    /// Get RTL (Right-to-Left) direction for current culture
    /// </summary>
    bool IsRightToLeft { get; }

    /// <summary>
    /// Event fired when culture changes
    /// </summary>
    event EventHandler<CultureChangedEventArgs> CultureChanged;
}

/// <summary>
/// Culture changed event arguments
/// </summary>
public class CultureChangedEventArgs : EventArgs
{
    public CultureInfo OldCulture { get; set; }
    public CultureInfo NewCulture { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Advanced localization service implementation
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILocalizationRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocalizationService> _logger;
    private readonly LocalizationConfiguration _config;
    private CultureInfo _currentCulture;
    private readonly ConcurrentDictionary<string, LocalizationResource> _resources = new();

    public event EventHandler<CultureChangedEventArgs> CultureChanged;

    public bool IsRightToLeft => _currentCulture.TextInfo.IsRightToLeft;

    public LocalizationService(
        ILocalizationRepository repository,
        IMemoryCache cache,
        ILogger<LocalizationService> logger,
        IOptions<LocalizationConfiguration> config)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
        _currentCulture = new CultureInfo(_config.DefaultCulture);
    }

    public string GetString(string key, params object[] args)
    {
        return GetString(key, _currentCulture, args);
    }

    public string GetString(string key, string fallback, params object[] args)
    {
        var result = GetString(key, _currentCulture, args);
        return result == key ? fallback : result;
    }

    public string GetString(string key, CultureInfo culture, params object[] args)
    {
        try
        {
            var cacheKey = $"localization:{culture.Name}:{key}";
            
            if (_cache.TryGetValue(cacheKey, out string cachedValue))
            {
                return FormatString(cachedValue, args);
            }

            var translation = GetTranslationFromResource(key, culture);
            
            if (translation != null)
            {
                _cache.Set(cacheKey, translation, TimeSpan.FromMinutes(_config.CacheMinutes));
                return FormatString(translation, args);
            }

            // Try fallback culture
            if (culture.Name != _config.DefaultCulture)
            {
                var fallbackCulture = new CultureInfo(_config.DefaultCulture);
                translation = GetTranslationFromResource(key, fallbackCulture);
                
                if (translation != null)
                {
                    _logger.LogWarning("Translation not found for key '{Key}' in culture '{Culture}', using fallback", key, culture.Name);
                    return FormatString(translation, args);
                }
            }

            // Log missing translation
            _logger.LogWarning("Translation not found for key '{Key}' in culture '{Culture}'", key, culture.Name);
            
            // Return key as fallback
            return FormatString(key, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized string for key '{Key}' in culture '{Culture}'", key, culture.Name);
            return key;
        }
    }

    public Dictionary<string, string> GetAllTranslations(string key)
    {
        var translations = new Dictionary<string, string>();
        
        foreach (var culture in GetSupportedCultures())
        {
            var translation = GetTranslationFromResource(key, culture);
            if (translation != null)
            {
                translations[culture.Name] = translation;
            }
        }
        
        return translations;
    }

    public async Task SetCultureAsync(string cultureName)
    {
        try
        {
            var newCulture = new CultureInfo(cultureName);
            var oldCulture = _currentCulture;
            
            _currentCulture = newCulture;
            
            // Set thread culture
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;
            
            // Set culture for async operations
            CultureInfo.DefaultThreadCurrentCulture = newCulture;
            CultureInfo.DefaultThreadCurrentUICulture = newCulture;
            
            // Load resources for new culture if not already loaded
            await LoadResourcesAsync(cultureName);
            
            // Fire event
            CultureChanged?.Invoke(this, new CultureChangedEventArgs
            {
                OldCulture = oldCulture,
                NewCulture = newCulture
            });
            
            _logger.LogInformation("Culture changed from {OldCulture} to {NewCulture}", oldCulture.Name, newCulture.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set culture to {CultureName}", cultureName);
            throw;
        }
    }

    public CultureInfo GetCurrentCulture()
    {
        return _currentCulture;
    }

    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        return _config.SupportedCultures.Select(c => new CultureInfo(c));
    }

    public bool Exists(string key)
    {
        return GetTranslationFromResource(key, _currentCulture) != null;
    }

    public double GetCompletionPercentage(string cultureName)
    {
        try
        {
            var totalKeys = GetAllKeysForCulture(_config.DefaultCulture).Count();
            var translatedKeys = GetAllKeysForCulture(cultureName).Count();
            
            return totalKeys > 0 ? (double)translatedKeys / totalKeys * 100 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate completion percentage for culture {CultureName}", cultureName);
            return 0;
        }
    }

    public IEnumerable<string> GetMissingTranslations(string cultureName)
    {
        try
        {
            var defaultKeys = GetAllKeysForCulture(_config.DefaultCulture);
            var cultureKeys = GetAllKeysForCulture(cultureName);
            
            return defaultKeys.Except(cultureKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get missing translations for culture {CultureName}", cultureName);
            return Enumerable.Empty<string>();
        }
    }

    public async Task AddOrUpdateTranslationAsync(string key, string cultureName, string value)
    {
        try
        {
            await _repository.AddOrUpdateTranslationAsync(key, cultureName, value);
            
            // Invalidate cache
            var cacheKey = $"localization:{cultureName}:{key}";
            _cache.Remove(cacheKey);
            
            // Reload resources
            await LoadResourcesAsync(cultureName);
            
            _logger.LogInformation("Translation updated for key '{Key}' in culture '{Culture}'", key, cultureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update translation for key '{Key}' in culture '{Culture}'", key, cultureName);
            throw;
        }
    }

    public async Task RemoveTranslationAsync(string key, string cultureName)
    {
        try
        {
            await _repository.RemoveTranslationAsync(key, cultureName);
            
            // Invalidate cache
            var cacheKey = $"localization:{cultureName}:{key}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Translation removed for key '{Key}' in culture '{Culture}'", key, cultureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove translation for key '{Key}' in culture '{Culture}'", key, cultureName);
            throw;
        }
    }

    public async Task ImportTranslationsAsync(string cultureName, Stream fileStream, string format = "json")
    {
        try
        {
            var translations = format.ToLower() switch
            {
                "json" => await ImportFromJsonAsync(fileStream),
                "csv" => await ImportFromCsvAsync(fileStream),
                "xlsx" => await ImportFromExcelAsync(fileStream),
                _ => throw new NotSupportedException($"Format '{format}' is not supported")
            };

            foreach (var (key, value) in translations)
            {
                await AddOrUpdateTranslationAsync(key, cultureName, value);
            }

            _logger.LogInformation("Imported {Count} translations for culture '{Culture}' from {Format}", 
                translations.Count, cultureName, format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import translations for culture '{Culture}' from {Format}", cultureName, format);
            throw;
        }
    }

    public async Task<Stream> ExportTranslationsAsync(string cultureName, string format = "json")
    {
        try
        {
            var translations = await _repository.GetTranslationsAsync(cultureName);
            
            return format.ToLower() switch
            {
                "json" => await ExportToJsonAsync(translations),
                "csv" => await ExportToCsvAsync(translations),
                "xlsx" => await ExportToExcelAsync(translations),
                _ => throw new NotSupportedException($"Format '{format}' is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export translations for culture '{Culture}' to {Format}", cultureName, format);
            throw;
        }
    }

    public string FormatCurrency(decimal amount, string currencyCode = null)
    {
        try
        {
            currencyCode ??= _config.DefaultCurrency;
            var regionInfo = new RegionInfo(_currentCulture.Name);
            
            return amount.ToString("C", _currentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format currency {Amount} for culture '{Culture}'", amount, _currentCulture.Name);
            return amount.ToString("C");
        }
    }

    public string FormatDate(DateTime date, string format = null)
    {
        try
        {
            format ??= _currentCulture.DateTimeFormat.ShortDatePattern;
            return date.ToString(format, _currentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format date {Date} for culture '{Culture}'", date, _currentCulture.Name);
            return date.ToString();
        }
    }

    public string FormatNumber(decimal number, int decimals = 2)
    {
        try
        {
            var format = "N" + decimals;
            return number.ToString(format, _currentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format number {Number} for culture '{Culture}'", number, _currentCulture.Name);
            return number.ToString();
        }
    }

    public string FormatPercentage(double percentage, int decimals = 2)
    {
        try
        {
            var format = "P" + decimals;
            return percentage.ToString(format, _currentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format percentage {Percentage} for culture '{Culture}'", percentage, _currentCulture.Name);
            return percentage.ToString();
        }
    }

    private string GetTranslationFromResource(string key, CultureInfo culture)
    {
        var resourceKey = culture.Name;
        
        if (_resources.TryGetValue(resourceKey, out var resource))
        {
            return resource.Translations.GetValueOrDefault(key);
        }
        
        return null;
    }

    private async Task LoadResourcesAsync(string cultureName)
    {
        try
        {
            var translations = await _repository.GetTranslationsAsync(cultureName);
            
            var resource = new LocalizationResource
            {
                Culture = cultureName,
                Translations = translations,
                LoadedAt = DateTime.UtcNow
            };
            
            _resources.AddOrUpdate(cultureName, resource, (key, oldValue) => resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resources for culture '{Culture}'", cultureName);
        }
    }

    private IEnumerable<string> GetAllKeysForCulture(string cultureName)
    {
        if (_resources.TryGetValue(cultureName, out var resource))
        {
            return resource.Translations.Keys;
        }
        
        return Enumerable.Empty<string>();
    }

    private string FormatString(string format, object[] args)
    {
        try
        {
            return args?.Length > 0 ? string.Format(format, args) : format;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format string '{Format}' with args", format);
            return format;
        }
    }

    private async Task<Dictionary<string, string>> ImportFromJsonAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private async Task<Dictionary<string, string>> ImportFromCsvAsync(Stream stream)
    {
        var translations = new Dictionary<string, string>();
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var parts = line.Split(',', 2);
            if (parts.Length == 2)
            {
                translations[parts[0].Trim('"')] = parts[1].Trim('"');
            }
        }
        
        return translations;
    }

    private async Task<Dictionary<string, string>> ImportFromExcelAsync(Stream stream)
    {
        // Implementation for Excel import
        // This would use a library like EPPlus or NPOI
        throw new NotImplementedException("Excel import not implemented yet");
    }

    private async Task<Stream> ExportToJsonAsync(Dictionary<string, string> translations)
    {
        var json = JsonSerializer.Serialize(translations, new JsonSerializerOptions { WriteIndented = true });
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
        stream.Position = 0;
        return stream;
    }

    private async Task<Stream> ExportToCsvAsync(Dictionary<string, string> translations)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        
        foreach (var (key, value) in translations)
        {
            await writer.WriteLineAsync($"\"{key}\",\"{value}\"");
        }
        
        await writer.FlushAsync();
        stream.Position = 0;
        return stream;
    }

    private async Task<Stream> ExportToExcelAsync(Dictionary<string, string> translations)
    {
        // Implementation for Excel export
        // This would use a library like EPPlus or NPOI
        throw new NotImplementedException("Excel export not implemented yet");
    }
}

/// <summary>
/// Localization resource model
/// </summary>
public class LocalizationResource
{
    public string Culture { get; set; }
    public Dictionary<string, string> Translations { get; set; } = new();
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Localization configuration
/// </summary>
public class LocalizationConfiguration
{
    public string DefaultCulture { get; set; } = "en-US";
    public string DefaultCurrency { get; set; } = "USD";
    public string[] SupportedCultures { get; set; } = { "en-US", "es-ES", "fr-FR", "de-DE", "pt-BR", "zh-CN", "ja-JP", "ar-SA" };
    public int CacheMinutes { get; set; } = 60;
    public bool EnableRealTimeUpdates { get; set; } = true;
    public bool EnableAutoDetection { get; set; } = true;
    public bool EnableFallback { get; set; } = true;
}