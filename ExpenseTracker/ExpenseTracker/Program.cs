using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ExpenseTracker.Client.Pages;
using ExpenseTracker.Components;
using ExpenseTracker.Data;
using ExpenseTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Settings
var jwtSettings = new JwtSettings
{
    SecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-super-secret-key-change-this-in-production-at-least-32-chars-long!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "ExpenseTrackerApp",
    Audience = builder.Configuration["Jwt:Audience"] ?? "ExpenseTrackerUsers",
    ExpirationMinutes = int.Parse(builder.Configuration["Jwt:ExpirationMinutes"] ?? "60")
};

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Entity Framework Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    $"Data Source={Path.Combine(AppContext.BaseDirectory, "expensetracker.db")}";
builder.Services.AddDbContext<ExpenseTrackerContext>(options =>
    options.UseSqlite(connectionString));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient();

// Register JWT and Auth Services
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IAuthService, AuthService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("https://localhost:7000", "http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add Authentication
var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

// Migrate the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ExpenseTrackerContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Add CORS middleware
app.UseCors("AllowLocalhost");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ExpenseTracker.Client._Imports).Assembly);

app.MapControllers();

app.Run();
