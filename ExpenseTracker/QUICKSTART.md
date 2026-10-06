# Quick Start Guide - Expense Tracker

## Prerequisites
- .NET 10 SDK installed
- Visual Studio 2026 Community Edition or VS Code with C# extension

## Installation & Running

### Option 1: Using Visual Studio 2026

1. **Open the Solution**
   - Open `ExpenseTracker.slnx` in Visual Studio 2026

2. **Build the Solution**
   - Build → Build Solution (Ctrl+Shift+B)
   - Wait for build to complete

3. **Run the Application**
   - Press F5 or click "Start Debugging"
   - Application launches at `https://localhost:7000`

### Option 2: Using Command Line

1. **Navigate to Project**
   ```bash
   cd C:\Users\SiddharthNandiwada\source\repos\ExpenseTracker
   ```

2. **Build the Solution**
   ```bash
   dotnet build
   ```

3. **Run the Application**
   ```bash
   dotnet run --project ExpenseTracker\ExpenseTracker\ExpenseTracker.csproj
   ```

4. **Open in Browser**
   - Navigate to `https://localhost:7000`

## First-Time Setup

### 1. Create Account
- Click "Register" tab
- Enter username, email, password
- Click "Register"

### 2. Login
- Switch to "Login" tab
- Enter your credentials
- Click "Login"

### 3. You're Now Authenticated!
- JWT token is stored in browser localStorage
- You see the dashboard with monthly summary
- Navigation menu appears

## Using the Application

### Dashboard
- Shows current month's financial summary
- Displays income, expenses, balance
- Shows top spending categories
- Switch months using the date selector

### Add Expense
1. Click "Expenses" in navigation menu
2. Click "Add Expense" button
3. Fill in details:
   - Description (required)
   - Amount (required)
   - Category (required)
   - Date (optional, defaults to today)
   - Notes (optional)
4. Click "Add" button

### View & Edit Expenses
- Expenses table shows all your transactions
- Click "Edit" to modify
- Click "Delete" to remove (with confirmation)
- Filter by date range using date inputs

### Add Income
1. Click "Income" in navigation menu
2. Click "Add Income" button
3. Fill in similar to expenses
4. Click "Add" button

### Monthly Summary
- Dashboard automatically loads current month
- Use month/year selector to view other months
- See all statistics by category
- View savings rate and average daily spending

## Testing JWT Authentication

### Test Scenario 1: Verify Token Storage
1. Open Browser Developer Tools (F12)
2. Go to Application → Local Storage
3. Find key: `authToken`
4. You'll see your JWT token

### Test Scenario 2: Verify User Isolation
1. Create two different accounts
2. Add expenses with Account A
3. Login with Account B
4. You won't see Account A's expenses
5. This proves user isolation works!

### Test Scenario 3: Test Expired Token
1. Set JWT expiration to 1 minute in `appsettings.json`:
   ```json
   "ExpirationMinutes": 1
   ```
2. Login and wait > 1 minute without activity
3. Try to add an expense
4. Application should redirect to login
5. This shows token validation works!

### Test Scenario 4: Verify Authorization Header
1. Open Browser Developer Tools
2. Go to Network tab
3. Make any API call (like loading expenses)
4. Click on the request
5. Go to Headers tab
6. Look for: `Authorization: Bearer eyJhb...`
7. This confirms token is being sent!

## Understanding JWT Flow

### Path: Login → Token Generation → Authorized Request

```
User Types Credentials
		 ↓
[POST] /api/auth/login
		 ↓
Server validates password with BCrypt
		 ↓
Server generates JWT token:
  - Header: {"alg": "HS256", "typ": "JWT"}
  - Payload: {"sub": "1", "name": "john", ...}
  - Signature: HMACSHA256(header.payload, secret)
		 ↓
Token returned to browser
		 ↓
Token stored in localStorage
		 ↓
Next API request includes:
  Authorization: Bearer [token]
		 ↓
Server validates token:
  - Checks signature
  - Checks expiration
  - Extracts claims
		 ↓
Request allowed, data returned filtered by UserId from JWT
```

## Important Files to Review

### Backend Authentication
- `ExpenseTracker/Services/AuthService.cs` - Token generation
- `ExpenseTracker/Controllers/AuthController.cs` - Login/Register endpoints
- `ExpenseTracker/Program.cs` - JWT middleware setup

### Frontend Authentication  
- `ExpenseTracker.Client/Services/AuthService.cs` - Token management
- `ExpenseTracker.Client/Services/AuthorizingHttpMessageHandler.cs` - Token injection
- `ExpenseTracker.Client/Pages/Login.razor` - Login/Register UI

### Protected Endpoints
- `ExpenseTracker/Controllers/ExpensesController.cs` - Shows `[Authorize]` pattern
- `ExpenseTracker/Controllers/IncomesController.cs` - Another protected controller

## Common Issues & Solutions

### Issue: "Build Failed" Error
**Solution:**
- Run `dotnet restore` to restore packages
- Ensure all NuGet packages are properly installed
- Delete `bin` and `obj` folders, rebuild

### Issue: Database Lock Error
**Solution:**
- Close any other instances of the app
- Delete `expensetracker.db` file
- Application will recreate it on next run

### Issue: Token Not Persisting Between Sessions
**Solution:**
- Browser localStorage might be cleared
- Check if browser is in private/incognito mode
- Check browser settings for localStorage restrictions

### Issue: CORS Errors
**Solution:**
- CORS is configured for localhost
- If running on different URLs, update `Program.cs` CORS policy
- Ensure API and client run on configured URLs

### Issue: Unauthorized (401) on Protected Endpoints
**Solution:**
- Token might be invalid or expired
- Try logging out and logging back in
- Check if token exists in localStorage
- Verify JWT secret key is correct

## Performance Tips

### Loading Data
- First load might take a few seconds
- Subsequent loads are cached
- Refresh page to get latest data

### Adding Multiple Transactions
- Batch operations work well
- Add expenses then refresh once

### Monthly Summary
- Calculated server-side (should be fast)
- Caches category summaries
- Change month to see different periods

## Next Steps

1. **Explore the Code**
   - Review AuthService.cs to understand token generation
   - Check ExpensesController.cs for user isolation pattern
   - Study Program.cs for middleware setup

2. **Implement Features**
   - Try adding recurring transactions
   - Implement budget alerts
   - Add expense forecasting

3. **Enhance Security**
   - Implement refresh tokens
   - Add two-factor authentication
   - Enable encrypted database

4. **Deploy**
   - Deploy to Azure App Service
   - Use Azure SQL Database
   - Enable HTTPS only

## Getting Help

### Error Messages
- Check browser console (F12 → Console)
- Check browser network tab (F12 → Network)
- Check server output in Visual Studio Output window

### API Testing
- Use Postman or Thunder Client to test endpoints
- Test without JWT token on protected endpoints
- Test with invalid tokens

### Debugging
- Add breakpoints in Visual Studio
- Use browser debugger (F12 → Sources)
- Check database with SQLite Browser

## Key Concepts to Practice

1. **JWT Authentication** - How tokens replace sessions
2. **Bearer Token** - `Authorization: Bearer [token]` pattern
3. **Claims** - User data embedded in token
4. **User Isolation** - Filtering data by authenticated user
5. **CORS** - Cross-Origin Resource Sharing
6. **localStorage** - Client-side token storage
7. **HttpClient Interceptor** - Automatic header injection

---

**Start by creating an account and adding your first expense!** 🎯
