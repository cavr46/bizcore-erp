namespace BizCore.Web.Services;

public interface IUserService
{
    Task<UserInfo?> GetCurrentUserAsync();
    Task<bool> GetDarkModePreferenceAsync();
    Task SetDarkModePreferenceAsync(bool isDarkMode);
    Task LogoutAsync();
}

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public UserService(
        ILogger<UserService> logger,
        ILocalStorageService localStorage,
        HttpClient httpClient)
    {
        _logger = logger;
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            // In a real implementation, this would decode JWT token or call an API
            var user = await _localStorage.GetItemAsync<UserInfo?>("currentUser");
            return user ?? new UserInfo
            {
                Id = Guid.NewGuid(),
                Name = "Demo User",
                Email = "demo@bizcore.com",
                Role = "Administrator",
                TenantId = "demo"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    public async Task<bool> GetDarkModePreferenceAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<bool>("darkMode");
        }
        catch
        {
            return false;
        }
    }

    public async Task SetDarkModePreferenceAsync(bool isDarkMode)
    {
        try
        {
            await _localStorage.SetItemAsync("darkMode", isDarkMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dark mode preference");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync("currentUser");
            await _localStorage.RemoveItemAsync("authToken");
            _logger.LogInformation("User logged out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime LastLogin { get; set; }
    public Dictionary<string, string> Permissions { get; set; } = new();
}

// Mock interface for Blazored.LocalStorage
public interface ILocalStorageService
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
}