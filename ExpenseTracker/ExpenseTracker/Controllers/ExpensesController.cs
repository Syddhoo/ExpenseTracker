using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseTrackerContext _context;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(ExpenseTrackerContext context, ILogger<ExpensesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetUserId();

            var query = _context.Expenses
                .Where(e => e.UserId == userId)
                .Include(e => e.Category)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.TransactionDate <= endDate.Value);

            var expenses = await query
                .OrderByDescending(e => e.TransactionDate)
                .ToListAsync();

            var expenseDtos = expenses.Select(e => new ExpenseDto
            {
                Id = e.Id,
                Description = e.Description,
                Amount = e.Amount,
                TransactionDate = e.TransactionDate,
                CategoryId = e.CategoryId,
                CategoryName = e.Category?.Name,
                Notes = e.Notes,
                CreatedAt = e.CreatedAt
            }).ToList();

            return Ok(expenseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expenses");
            return StatusCode(500, new { message = "An error occurred while fetching expenses" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseDto>> GetExpense(int id)
    {
        try
        {
            var userId = GetUserId();
            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (expense == null)
                return NotFound(new { message = "Expense not found" });

            var expenseDto = new ExpenseDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                TransactionDate = expense.TransactionDate,
                CategoryId = expense.CategoryId,
                CategoryName = expense.Category?.Name,
                Notes = expense.Notes,
                CreatedAt = expense.CreatedAt
            };

            return Ok(expenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expense");
            return StatusCode(500, new { message = "An error occurred while fetching the expense" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Description) || request.Amount <= 0)
                return BadRequest(new { message = "Description and amount are required, amount must be greater than 0" });

            var userId = GetUserId();

            // Verify category belongs to user
            var category = await _context.Categories.FirstOrDefaultAsync(
                c => c.Id == request.CategoryId && c.UserId == userId);

            if (category == null)
                return BadRequest(new { message = "Invalid category" });

            var expense = new Expense
            {
                Description = request.Description,
                Amount = request.Amount,
                TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
                UserId = userId,
                CategoryId = request.CategoryId,
                Notes = request.Notes ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense created for user {userId}");

            var expenseDto = new ExpenseDto
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                TransactionDate = expense.TransactionDate,
                CategoryId = expense.CategoryId,
                CategoryName = category.Name,
                Notes = expense.Notes,
                CreatedAt = expense.CreatedAt
            };

            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { message = "An error occurred while creating the expense" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            var userId = GetUserId();
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (expense == null)
                return NotFound(new { message = "Expense not found" });

            if (!string.IsNullOrWhiteSpace(request.Description))
                expense.Description = request.Description;

            if (request.Amount.HasValue && request.Amount > 0)
                expense.Amount = request.Amount.Value;

            if (request.TransactionDate.HasValue)
                expense.TransactionDate = request.TransactionDate.Value;

            if (request.CategoryId.HasValue)
            {
                // Verify new category belongs to user
                var category = await _context.Categories.FirstOrDefaultAsync(
                    c => c.Id == request.CategoryId && c.UserId == userId);

                if (category == null)
                    return BadRequest(new { message = "Invalid category" });

                expense.CategoryId = request.CategoryId.Value;
            }

            if (request.Notes != null)
                expense.Notes = request.Notes;

            expense.UpdatedAt = DateTime.UtcNow;

            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense {id} updated for user {userId}");

            return Ok(new { message = "Expense updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            return StatusCode(500, new { message = "An error occurred while updating the expense" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        try
        {
            var userId = GetUserId();
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (expense == null)
                return NotFound(new { message = "Expense not found" });

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense {id} deleted for user {userId}");

            return Ok(new { message = "Expense deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            return StatusCode(500, new { message = "An error occurred while deleting the expense" });
        }
    }

    [HttpGet("summary/monthly")]
    public async Task<ActionResult<MonthlySummaryDto>> GetMonthlySummary([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var userId = GetUserId();
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var expenses = await _context.Expenses
                .Where(e => e.UserId == userId && 
                           e.TransactionDate >= startDate && 
                           e.TransactionDate <= endDate)
                .Include(e => e.Category)
                .ToListAsync();

            var incomes = await _context.Incomes
                .Where(i => i.UserId == userId && 
                           i.TransactionDate >= startDate && 
                           i.TransactionDate <= endDate)
                .Include(i => i.Category)
                .ToListAsync();

            var totalExpenses = expenses.Sum(e => e.Amount);
            var totalIncomes = incomes.Sum(i => i.Amount);
            var balance = totalIncomes - totalExpenses;

            var expensesByCategory = expenses
                .GroupBy(e => e.Category?.Name ?? "Uncategorized")
                .Select(g => new CategorySummaryDto
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .ToList();

            return Ok(new MonthlySummaryDto
            {
                Year = year,
                Month = month,
                TotalExpenses = totalExpenses,
                TotalIncomes = totalIncomes,
                Balance = balance,
                ExpensesByCategory = expensesByCategory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching monthly summary");
            return StatusCode(500, new { message = "An error occurred while fetching the monthly summary" });
        }
    }
}

// DTOs
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
