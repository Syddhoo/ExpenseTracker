# Expense Tracker - JWT Authorization Learning Application

A full-stack Blazor WebAssembly application demonstrating JWT (JSON Web Token) authentication and authorization for personal expense tracking. This project is designed as a learning tool to understand how JWT tokens work in a real-world application.

## Project Overview

Expense Tracker is a complete application that enables users to:
- **Register & Login** - Create accounts and authenticate with JWT tokens
- **Track Expenses** - Record spending with categories and notes
- **Track Income** - Record income sources
- **View Summaries** - Monthly expense analysis and summaries
- **Categorize Transactions** - Organize expenses and income by category
- **Dashboard Analytics** - View balance, spending patterns, and financial overview

## Technology Stack

### Backend
- **ASP.NET Core 10.0** - Web API framework
- **Entity Framework Core 10.0** - ORM for database operations
- **SQLite** - Local database
- **JWT Bearer Authentication** - Secure token-based authentication
- **BCrypt.Net** - Password hashing

### Frontend
- **Blazor WebAssembly** - Client-side C# framework
- **Bootstrap 5** - UI styling
- **Blazored LocalStorage** - Client-side token persistence
- **HttpClient** - API communication

## Architecture

### JWT Flow Diagram
```
1. User Registration/Login
   └─> Backend validates credentials
   └─> Backend generates JWT token with user claims
   └─> Token returned to frontend (stored in localStorage)

2. Authenticated Requests
   └─> Frontend retrieves token from localStorage
   └─> AuthorizingHttpMessageHandler injects token in Authorization header
   └─> Backend validates token with [Authorize] attribute
   └─> User claims extracted from token via User.FindFirst(ClaimTypes.NameIdentifier)
   └─> API returns only user's data

3. Data Security
   └─> Each expense/income record linked to UserId from JWT
   └─> Database queries filtered by authenticated user's ID
   └─> Prevents unauthorized access to other users' data
```

## Key Features & Learning Points

### 1. JWT Token Generation
**File:** `ExpenseTracker/Services/AuthService.cs`

The `AuthService` generates JWT tokens with the following:
- **NameIdentifier Claim**: User ID for extracting user identity
- **Name Claim**: Username
- **Email Claim**: User email
- **Custom Claim**: UserId for convenience

```csharp
var claims = new List<Claim>
{
	new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
	new Claim(ClaimTypes.Name, user.Username),
	new Claim(ClaimTypes.Email, user.Email),
	new Claim("UserId", user.Id.ToString())
};
```

### 2. Token Validation & Middleware
**File:** `ExpenseTracker/Program.cs`

JWT Bearer authentication is configured with:
- Issuer & Audience validation
- Signature verification
- Token expiration checking

```csharp
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
```

### 3. Protected API Endpoints
**File:** `ExpenseTracker/Controllers/ExpensesController.cs`

All expense endpoints are protected with `[Authorize]` attribute. User identity is extracted:

```csharp
private int GetUserId()
{
	var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	if (int.TryParse(userIdClaim, out var userId))
	{
		return userId;
	}
	throw new UnauthorizedAccessException("User ID not found in token");
}
```

Every database query is filtered by the authenticated user's ID:
```csharp
var expenses = await _context.Expenses
	.Where(e => e.UserId == userId)  // Ensures user isolation
	.ToListAsync();
```

### 4. Client-Side Token Management
**File:** `ExpenseTracker.Client/Services/AuthService.cs`

- Stores JWT token in browser's localStorage
- Provides login/register/logout functionality
- Manages authentication state

**File:** `ExpenseTracker.Client/Services/AuthorizingHttpMessageHandler.cs`

- Automatic token injection in all API requests
- Adds `Authorization: Bearer <token>` header
- Handles token retrieval from localStorage

```csharp
protected override async Task<HttpResponseMessage> SendAsync(
	HttpRequestMessage request,
	CancellationToken cancellationToken)
{
	var token = await _localStorage.GetItemAsStringAsync("authToken", cancellationToken);
	if (!string.IsNullOrEmpty(token))
	{
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
	}
	return await base.SendAsync(request, cancellationToken);
}
```

### 5. User Isolation Pattern
The application demonstrates complete user isolation:

**Database Level:**
- User model has navigation properties to Expenses, Incomes, and Categories
- Foreign key constraints ensure data integrity
- Default cascade delete for user's transactions

**API Level:**
- `[Authorize]` attribute on all protected endpoints
- User ID extracted from JWT claims
- All queries filtered by UserId

**Frontend Level:**
- Token stored locally
- Automatic logout if token expires
- Redirect to login page for unauthenticated access

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login (returns JWT token)

### Expenses (Requires [Authorize])
- `GET /api/expenses` - Get user's expenses
- `GET /api/expenses/{id}` - Get single expense
- `POST /api/expenses` - Create expense
- `PUT /api/expenses/{id}` - Update expense
- `DELETE /api/expenses/{id}` - Delete expense
- `GET /api/expenses/summary/monthly?year=2024&month=12` - Monthly summary

### Incomes (Requires [Authorize])
- `GET /api/incomes` - Get user's income records
- `GET /api/incomes/{id}` - Get single income
- `POST /api/incomes` - Create income
- `PUT /api/incomes/{id}` - Update income
- `DELETE /api/incomes/{id}` - Delete income

## Default Categories

The application creates default categories for each new user:

**Expense Categories:**
- Food & Dining
- Transportation
- Shopping
- Entertainment
- Utilities
- Healthcare
- Education
- Other

**Income Categories:**
- Salary
- Investments
- Other

## Configuration

### JWT Settings (appsettings.json)
```json
{
  "Jwt": {
	"SecretKey": "your-super-secret-key-change-this-in-production-at-least-32-chars-long!",
	"Issuer": "ExpenseTrackerApp",
	"Audience": "ExpenseTrackerUsers",
	"ExpirationMinutes": 60
  },
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=expensetracker.db"
  }
}
```

⚠️ **IMPORTANT**: Change the SecretKey before deploying to production!

## Running the Application

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or VS Code

### Build & Run
```bash
cd ExpenseTracker
dotnet build
dotnet run
```

The application will:
1. Create SQLite database automatically
2. Run migrations on startup
3. Start server on `https://localhost:7000`
4. Blazor WebAssembly client loads in browser

### First Time Setup
1. Navigate to the application
2. Click "Register" to create a new account
3. Login with your credentials
4. View the dashboard
5. Add expenses and income transactions
6. Check monthly summaries

## Learning Path

### Understanding JWT in this Application

1. **Token Creation** (AuthService.cs)
   - User provides credentials
   - Password verified with BCrypt
   - JWT created with user claims

2. **Token Storage** (Client AuthService)
   - Token saved to localStorage
   - Available for subsequent requests

3. **Token Usage** (AuthorizingHttpMessageHandler)
   - Automatically attached to all API requests
   - Sent in Authorization header

4. **Token Validation** (Program.cs)
   - Server validates signature
   - Server validates claims & expiration
   - User identity extracted

5. **Data Isolation** (ExpensesController)
   - User ID from token used to filter data
   - Only user's own data is accessible

### Code Review Checklist

- [ ] Review `AuthService.cs` to understand token generation
- [ ] Check `AuthorizingHttpMessageHandler.cs` for token injection
- [ ] Study `ExpensesController.cs` for user isolation pattern
- [ ] Examine `Program.cs` for JWT middleware configuration
- [ ] Test expired token behavior
- [ ] Test accessing other user's data (should fail)
- [ ] Review localStorage usage in browser DevTools

## Security Considerations

### Current Implementation (Learning Level)
- ✅ Passwords hashed with BCrypt
- ✅ JWT tokens signed and validated
- ✅ User isolation enforced
- ✅ HTTPS recommended

### Production Improvements Needed
- 🔒 Use HTTPS in production
- 🔒 Implement refresh tokens
- 🔒 Add rate limiting
- 🔒 Implement CORS more strictly
- 🔒 Add database encryption
- 🔒 Implement audit logging
- 🔒 Add two-factor authentication
- 🔒 Use environment-specific secrets

## Project Structure

```
ExpenseTracker/
├── ExpenseTracker/                 # Backend Project
│   ├── Controllers/
│   │   ├── AuthController.cs       # Login & Register
│   │   ├── ExpensesController.cs   # Protected Expenses endpoints
│   │   └── IncomesController.cs    # Protected Incomes endpoints
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Expense.cs
│   │   ├── Income.cs
│   │   └── Category.cs
│   ├── Data/
│   │   ├── ExpenseTrackerContext.cs
│   │   └── ExpenseTrackerContextFactory.cs
│   ├── Services/
│   │   └── AuthService.cs          # JWT generation & password hashing
│   ├── Program.cs                  # Service configuration & middleware
│   └── appsettings.json            # JWT configuration
│
└── ExpenseTracker.Client/          # Blazor WebAssembly Client
	├── Pages/
	│   ├── Login.razor             # Auth UI
	│   ├── Dashboard.razor         # Main dashboard
	│   ├── Expenses.razor          # Expense management
	│   └── Incomes.razor           # Income management
	├── Services/
	│   ├── AuthService.cs          # Token management
	│   ├── AuthorizingHttpMessageHandler.cs
	│   └── TransactionService.cs   # API calls
	└── Program.cs                  # Client service configuration
```

## Troubleshooting

### "Invalid token" errors
- Check if JWT secret key matches between appsettings.json and code
- Verify token expiration time
- Check token wasn't modified in localStorage

### "User isolation not working"
- Ensure `[Authorize]` attribute is on controller
- Verify UserId is correctly extracted from claims
- Check database queries filter by UserId

### CORS errors
- Update CORS policy in Program.cs with correct origins
- Ensure credentials are sent with requests

## Next Steps for Learning

1. **Implement Refresh Tokens** - Add token refresh without relogging
2. **Add Roles/Claims** - Differentiate between admin and regular users
3. **Implement Audit Logging** - Log all financial transactions
4. **Add API Rate Limiting** - Protect against abuse
5. **Implement Two-Factor Authentication** - Enhance security
6. **Create Charts** - Add visual expense analytics
7. **Export Reports** - Generate PDF/Excel reports

## Resources

- [JWT Introduction](https://jwt.io/introduction)
- [ASP.NET Core JWT Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt)
- [Blazor WebAssembly Docs](https://docs.microsoft.com/en-us/aspnet/core/blazor)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [BCrypt Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

## License

This project is created for educational purposes to learn JWT authentication and .NET development.

---

**Happy Learning! 🚀**

This expense tracker demonstrates real-world JWT usage patterns that you can apply to any web application requiring secure user authentication.
