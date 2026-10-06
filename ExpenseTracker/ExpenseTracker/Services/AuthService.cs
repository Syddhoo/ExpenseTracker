using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public interface IAuthService
{
    string GenerateJwtToken(User user);
    bool VerifyPassword(string password, string hash);
    string HashPassword(string password);
}

public class AuthService : IAuthService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(JwtSettings jwtSettings, ILogger<AuthService> logger)
    {
        _jwtSettings = jwtSettings;
        _logger = logger;
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrEmpty(_jwtSettings.SecretKey) ||
            string.IsNullOrEmpty(_jwtSettings.Issuer) ||
            string.IsNullOrEmpty(_jwtSettings.Audience))
        {
            throw new InvalidOperationException("JWT settings are not configured properly");
        }
    }

    public string GenerateJwtToken(User user)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token");
            throw;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
