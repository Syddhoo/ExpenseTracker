using Blazored.LocalStorage;

namespace ExpenseTracker.Client.Services;

public class AuthorizingHttpMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthorizingHttpMessageHandler> _logger;

    public AuthorizingHttpMessageHandler(
        ILocalStorageService localStorage,
        ILogger<AuthorizingHttpMessageHandler> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken", cancellationToken);

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);
                _logger.LogInformation("Authorization header added to request");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token from local storage");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
