using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using ExpenseTracker.Services;

namespace ExpenseTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ExpenseTrackerContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ExpenseTrackerContext context,
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username, email, and password are required" });
            }

            // Check if user already exists
            if (_context.Users.Any(u => u.Username == request.Username.ToLower()))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            if (_context.Users.Any(u => u.Email == request.Email.ToLower()))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Create new user
            var user = new User
            {
                Username = request.Username.ToLower(),
                Email = request.Email.ToLower(),
                PasswordHash = _authService.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create default categories for new user
            await CreateDefaultCategories(user.Id);

            _logger.LogInformation($"User {user.Username} registered successfully");

            return Ok(new { message = "User registered successfully", userId = user.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username.ToLower());

            if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning($"Failed login attempt for username: {request.Username}");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "User account is inactive" });
            }

            var token = _authService.GenerateJwtToken(user);

            _logger.LogInformation($"User {user.Username} logged in successfully");

            return Ok(new
            {
                token = token,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    private async Task CreateDefaultCategories(int userId)
    {
        var defaultCategories = new[]
        {
            new { Name = "Food & Dining", Color = "#FF6B6B" },
            new { Name = "Transportation", Color = "#4ECDC4" },
            new { Name = "Shopping", Color = "#95E1D3" },
            new { Name = "Entertainment", Color = "#F38181" },
            new { Name = "Utilities", Color = "#AA96DA" },
            new { Name = "Healthcare", Color = "#FCBAD3" },
            new { Name = "Education", Color = "#A8D8EA" },
            new { Name = "Other", Color = "#D4A5A5" },
            new { Name = "Salary", Color = "#52B788" },
            new { Name = "Investments", Color = "#FFB703" }
        };

        foreach (var cat in defaultCategories)
        {
            _context.Categories.Add(new Category
            {
                Name = cat.Name,
                UserId = userId,
                Color = cat.Color,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
