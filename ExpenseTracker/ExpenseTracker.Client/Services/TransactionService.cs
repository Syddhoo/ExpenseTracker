using System.Text.Json;

namespace ExpenseTracker.Client.Services;

public class ExpenseDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateExpenseRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public int CategoryId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateExpenseRequest
{
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
}

public class IncomeDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateIncomeRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public int CategoryId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateIncomeRequest
{
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
}

public class MonthlySummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalIncomes { get; set; }
    public decimal Balance { get; set; }
    public List<CategorySummaryDto> ExpensesByCategory { get; set; } = [];
}

public class CategorySummaryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class TransactionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(HttpClient httpClient, ILogger<TransactionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Expense methods
    public async Task<List<ExpenseDto>> GetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var url = "api/expenses";
            var queryParams = new List<string>();

            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:O}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:O}");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var expenses = JsonSerializer.Deserialize<List<ExpenseDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return expenses ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expenses");
            return [];
        }
    }

    public async Task<ExpenseDto?> GetExpenseAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/expenses/{id}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var expense = JsonSerializer.Deserialize<ExpenseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expense");
            return null;
        }
    }

    public async Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/expenses", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var expense = JsonSerializer.Deserialize<ExpenseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return expense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return null;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int id, UpdateExpenseRequest request)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/expenses/{id}", content);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/expenses/{id}");
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            return false;
        }
    }

    // Income methods
    public async Task<List<IncomeDto>> GetIncomesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var url = "api/incomes";
            var queryParams = new List<string>();

            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:O}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:O}");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var incomes = JsonSerializer.Deserialize<List<IncomeDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return incomes ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching incomes");
            return [];
        }
    }

    public async Task<IncomeDto?> GetIncomeAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/incomes/{id}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var income = JsonSerializer.Deserialize<IncomeDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return income;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching income");
            return null;
        }
    }

    public async Task<IncomeDto?> CreateIncomeAsync(CreateIncomeRequest request)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/incomes", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var income = JsonSerializer.Deserialize<IncomeDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return income;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating income");
            return null;
        }
    }

    public async Task<bool> UpdateIncomeAsync(int id, UpdateIncomeRequest request)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/incomes/{id}", content);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating income");
            return false;
        }
    }

    public async Task<bool> DeleteIncomeAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/incomes/{id}");
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting income");
            return false;
        }
    }

    public async Task<MonthlySummaryDto?> GetMonthlySummaryAsync(int year, int month)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/expenses/summary/monthly?year={year}&month={month}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var summary = JsonSerializer.Deserialize<MonthlySummaryDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching monthly summary");
            return null;
        }
    }
}
