# JWT Token Deep Dive - Understanding the Token Structure

## What is JWT?

JWT (JSON Web Token) is a stateless authentication mechanism. Instead of storing session data on the server, the server signs a token containing user information and sends it to the client. The client sends this token with every request, and the server validates the signature to trust the data.

## Token Anatomy

A JWT consists of three parts separated by dots: `header.payload.signature`

### Example Token
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1IiwibmJmIjoxNzMzNjI0MDAwLCJleHAiOjE3MzM2Mjc2MDAsImlhdCI6MTczMzYyNDAwMCwiaXNzIjoiRXhwZW5zZVRyYWNrZXJBcHAiLCJhdWQiOiJFeHBlbnNlVHJhY2tlclVzZXJzIiwiPSI1In0.xyz...
```

### Part 1: Header (Decoded)
```json
{
  "alg": "HS256",    // Algorithm (HMAC SHA256)
  "typ": "JWT"       // Type
}
```

### Part 2: Payload/Claims (Decoded)
```json
{
  "NameIdentifier": "5",        // User ID (from ClaimTypes.NameIdentifier)
  "Name": "john_doe",           // Username (from ClaimTypes.Name)
  "Email": "john@example.com",  // Email
  "UserId": "5",                // Custom claim
  "nbf": 1733624000,            // Not before time
  "exp": 1733627600,            // Expiration time (1 hour)
  "iat": 1733624000,            // Issued at time
  "iss": "ExpenseTrackerApp",   // Issuer
  "aud": "ExpenseTrackerUsers"  // Audience
}
```

### Part 3: Signature
```
HMACSHA256(
  base64UrlEncode(header) + "." +
  base64UrlEncode(payload),
  "your-super-secret-key-change-this-in-production-at-least-32-chars-long!"
)
```

## How the Application Uses JWT

### Step 1: Token Generation (AuthService.cs)

```csharp
public string GenerateJwtToken(User user)
{
	// 1. Create signing credentials
	var securityKey = new SymmetricSecurityKey(
		Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
	);
	var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

	// 2. Create claims (data embedded in token)
	var claims = new List<Claim>
	{
		new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // "5"
		new Claim(ClaimTypes.Name, user.Username),                  // "john_doe"
		new Claim(ClaimTypes.Email, user.Email),                    // "john@example.com"
		new Claim("UserId", user.Id.ToString())                     // "5"
	};

	// 3. Create token
	var token = new JwtSecurityToken(
		issuer: _jwtSettings.Issuer,              // "ExpenseTrackerApp"
		audience: _jwtSettings.Audience,          // "ExpenseTrackerUsers"
		claims: claims,
		expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
		signingCredentials: credentials
	);

	// 4. Return encoded token
	return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Output:**  A long Base64-encoded string like:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1IiwibmFtZSI6ImpvaG5fZG9lIiwi...
```

### Step 2: Token Storage (Client AuthService)

```csharp
public async Task<bool> LoginAsync(string username, string password)
{
	// 1. Send credentials to API
	var response = await _httpClient.PostAsync("api/auth/login", loginContent);

	// 2. Parse response
	var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse);

	// 3. Store token in localStorage
	await _localStorage.SetItemAsStringAsync("authToken", authResponse.Token);

	// 4. Token now available for future requests
	return true;
}
```

**localStorage now contains:**
```
Key: "authToken"
Value: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1IiwibmFtZSI6ImpvaG5fZG9lIiwi..."
```

### Step 3: Token Injection (AuthorizingHttpMessageHandler)

```csharp
protected override async Task<HttpResponseMessage> SendAsync(
	HttpRequestMessage request,
	CancellationToken cancellationToken)
{
	// 1. Retrieve token from localStorage
	var token = await _localStorage.GetItemAsStringAsync("authToken", cancellationToken);

	// 2. Add to Authorization header
	if (!string.IsNullOrEmpty(token))
	{
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
	}

	// 3. Token is now sent with every API request
	return await base.SendAsync(request, cancellationToken);
}
```

**Request Header:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1IiwibmFtZSI6ImpvaG5fZG9lIiwi...
```

### Step 4: Token Validation (Program.cs Middleware)

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
		// 1. Verify signature using secret key
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(key),

		// 2. Verify issuer
		ValidateIssuer = true,
		ValidIssuer = jwtSettings.Issuer,

		// 3. Verify audience
		ValidateAudience = true,
		ValidAudience = jwtSettings.Audience,

		// 4. Check expiration
		ValidateLifetime = true,
		ClockSkew = TimeSpan.Zero  // No grace period
	};
});
```

**Validation Process:**
1. ✅ Verify signature is valid (proves token wasn't tampered with)
2. ✅ Verify issuer is "ExpenseTrackerApp"
3. ✅ Verify audience is "ExpenseTrackerUsers"
4. ✅ Verify token hasn't expired
5. ✅ If all checks pass → Request is authenticated

### Step 5: Claims Extraction (ExpensesController.cs)

```csharp
private int GetUserId()
{
	// Extract user ID from JWT claims
	var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

	if (int.TryParse(userIdClaim, out var userId))
	{
		return userId;  // Returns "5" from token
	}
	throw new UnauthorizedAccessException("User ID not found in token");
}

[HttpGet]
public async Task<IEnumerable<ExpenseDto>> GetExpenses()
{
	var userId = GetUserId();  // Get "5" from token

	// Only return expenses where UserId == 5
	var expenses = await _context.Expenses
		.Where(e => e.UserId == userId)
		.ToListAsync();

	return expenses;
}
```

**Result:** User 5 can only see their own expenses!

---

## Complete Request Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User enters credentials and clicks Login                     │
├─────────────────────────────────────────────────────────────────┤
│ POST /api/auth/login                                            │
│ {                                                               │
│   "username": "john_doe",                                       │
│   "password": "password123"                                     │
│ }                                                               │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Backend: AuthController validates credentials               │
├─────────────────────────────────────────────────────────────────┤
│ ✓ Find user in database                                         │
│ ✓ Verify password with BCrypt                                   │
│ ✓ User valid → Continue                                         │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Backend: AuthService generates JWT token                    │
├─────────────────────────────────────────────────────────────────┤
│ Header:   {"alg": "HS256", "typ": "JWT"}                        │
│ Payload:  {                                                     │
│             "sub": "5",                                         │
│             "name": "john_doe",                                 │
│             "email": "john@example.com",                        │
│             "exp": 1733627600,                                  │
│             "iss": "ExpenseTrackerApp"                          │
│           }                                                     │
│ Signature: HMACSHA256(header.payload, secret_key)              │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Response sent to Frontend                                    │
├─────────────────────────────────────────────────────────────────┤
│ {                                                               │
│   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdW...",  │
│   "user": {                                                     │
│     "id": 5,                                                    │
│     "username": "john_doe",                                     │
│     "email": "john@example.com"                                 │
│   }                                                             │
│ }                                                               │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Frontend: Store token in localStorage                        │
├─────────────────────────────────────────────────────────────────┤
│ localStorage["authToken"] =                                     │
│   "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdW..."            │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. User navigates to /expenses                                  │
├─────────────────────────────────────────────────────────────────┤
│ Component calls: TransactionService.GetExpensesAsync()          │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. HttpClient makes request                                     │
├─────────────────────────────────────────────────────────────────┤
│ GET /api/expenses                                               │
│ [Request goes through AuthorizingHttpMessageHandler]            │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 8. AuthorizingHttpMessageHandler adds token                    │
├─────────────────────────────────────────────────────────────────┤
│ GET /api/expenses                                               │
│ Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..   │
│ [Request sent to server]                                        │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 9. Backend: JWT Middleware validates token                     │
├─────────────────────────────────────────────────────────────────┤
│ ✓ Decode token                                                  │
│ ✓ Verify signature matches secret_key                           │
│ ✓ Verify issuer = "ExpenseTrackerApp"                           │
│ ✓ Verify audience = "ExpenseTrackerUsers"                       │
│ ✓ Verify not expired                                            │
│ ✓ All checks pass → Request authenticated                       │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 10. ExpensesController receives request                         │
├─────────────────────────────────────────────────────────────────┤
│ [Authorize] attribute allows execution                          │
│ GetUserId() extracts userId = 5 from User.FindFirst()          │
│ Query database: WHERE UserId = 5                                │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 11. Response returned                                           │
├─────────────────────────────────────────────────────────────────┤
│ [                                                               │
│   {                                                             │
│     "id": 1,                                                    │
│     "description": "Grocery shopping",                          │
│     "amount": 45.50,                                            │
│     "categoryName": "Food & Dining"                             │
│   }                                                             │
│ ]                                                               │
└────────────────────────┬────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 12. Frontend displays expenses                                  │
├─────────────────────────────────────────────────────────────────┤
│ User sees only their expenses!                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Security Properties

### Why JWT is Secure

1. **Signature Verification**
   - Token can't be modified without invalidating signature
   - Only server with secret key can create valid tokens
   - If client modifies payload → Signature becomes invalid

2. **Claims Integrity**
   - Claims are NOT encrypted (base64 only)
   - But signature proves they haven't been modified
   - Server can trust claims are legitimate

3. **Stateless Verification**
   - No database lookup needed to verify token
   - Server doesn't store sessions
   - Scales well for distributed systems

4. **Time-Based Expiration**
   - Tokens automatically expire
   - Forces re-authentication periodically
   - Limits damage from stolen tokens

### What JWT Doesn't Provide

⚠️ **Not encrypted:** Claims are base64 encoded, not encrypted
- Don't put sensitive data in claims
- Client can read the payload (just can't modify it)

⚠️ **Not for storage:** Tokens don't replace storing user data
- Still need user records in database
- Token just proves user identity

⚠️ **Not for permissions alone:** Must validate on backend
- Don't trust client-side role checks
- Always verify with database on backend

---

## Testing JWT Tokens

### Decode Your Token

Go to https://jwt.io and paste your token:

1. Copy token from browser localStorage
2. Paste in jwt.io "Encoded" section
3. You'll see:
   - Header (decoded)
   - Payload (decoded)
   - Signature (unverified, since they don't have your secret key)

### Verify Token Tampering is Caught

1. Decode your token at jwt.io
2. Change one character in payload (e.g., change userId from "5" to "6")
3. Copy modified token to browser localStorage
4. Try to use app
5. App will reject: "Invalid signature"

### Test Expiration

1. In `appsettings.json`, change `ExpirationMinutes` to `1`
2. Rebuild and login
3. Copy token and check exp claim
4. Add: `exp: (current_timestamp + 60)` - token expires in 1 minute
5. Wait > 1 minute
6. Try to access protected endpoint
7. Request fails: "Token expired"

---

## Key Takeaways

1. ✅ JWT = Compact, self-contained way to verify user identity
2. ✅ Three parts: header.payload.signature
3. ✅ Signature proves token wasn't tampered with
4. ✅ Stored in localStorage, sent in Authorization header
5. ✅ Server validates signature using secret key
6. ✅ Claims extracted and used for authorization
7. ✅ Enables stateless authentication at scale

This is the foundation of modern API authentication! 🔐
