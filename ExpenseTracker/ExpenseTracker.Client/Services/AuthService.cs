using System.Text.Json;
using Blazored.LocalStorage;

namespace ExpenseTracker.Client.Services;

public class AuthResponse
{
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AuthService
{
    private const string TokenKey = "authToken";
    private const string UserKey = "currentUser";

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthService> _logger;

    public event Action? OnAuthStateChanged;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var request = new { username, email, password };
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Registration successful");
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Registration failed: {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return false;
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var request = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (authResponse?.Token != null && authResponse.User != null)
                {
                    await _localStorage.SetItemAsStringAsync(TokenKey, authResponse.Token);
                    await _localStorage.SetItemAsync(UserKey, authResponse.User);

                    _logger.LogInformation($"Login successful for user {username}");
                    OnAuthStateChanged?.Invoke();
                    return true;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Login failed: {errorContent}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(TokenKey);
            await _localStorage.RemoveItemAsync(UserKey);
            _logger.LogInformation("User logged out");
            OnAuthStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync(TokenKey);
            return !string.IsNullOrEmpty(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var user = await _localStorage.GetItemAsync<UserInfo>(UserKey);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return null;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _localStorage.GetItemAsStringAsync(TokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token");
            return null;
        }
    }
}
