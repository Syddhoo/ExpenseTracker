using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using ExpenseTracker.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add Logging - SetMinimumLevel is safe for WASM
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Local Storage
builder.Services.AddBlazoredLocalStorage();

// Register AuthService
builder.Services.AddScoped<AuthService>();

// Register TransactionService
builder.Services.AddScoped<TransactionService>();

// Add HttpClient with Authorization message handler
builder.Services.AddScoped<AuthorizingHttpMessageHandler>();
builder.Services.AddScoped(sp => 
{
    var handler = sp.GetRequiredService<AuthorizingHttpMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
    return client;
});

await builder.Build().RunAsync();
