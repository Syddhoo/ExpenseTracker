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
public class IncomesController : ControllerBase
{
    private readonly ExpenseTrackerContext _context;
    private readonly ILogger<IncomesController> _logger;

    public IncomesController(ExpenseTrackerContext context, ILogger<IncomesController> logger)
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
    public async Task<ActionResult<IEnumerable<IncomeDto>>> GetIncomes(
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetUserId();

            var query = _context.Incomes
                .Where(i => i.UserId == userId)
                .Include(i => i.Category)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(i => i.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(i => i.TransactionDate <= endDate.Value);

            var incomes = await query
                .OrderByDescending(i => i.TransactionDate)
                .ToListAsync();

            var incomeDtos = incomes.Select(i => new IncomeDto
            {
                Id = i.Id,
                Description = i.Description,
                Amount = i.Amount,
                TransactionDate = i.TransactionDate,
                CategoryId = i.CategoryId,
                CategoryName = i.Category?.Name,
                Notes = i.Notes,
                CreatedAt = i.CreatedAt
            }).ToList();

            return Ok(incomeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching incomes");
            return StatusCode(500, new { message = "An error occurred while fetching incomes" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IncomeDto>> GetIncome(int id)
    {
        try
        {
            var userId = GetUserId();
            var income = await _context.Incomes
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (income == null)
                return NotFound(new { message = "Income not found" });

            var incomeDto = new IncomeDto
            {
                Id = income.Id,
                Description = income.Description,
                Amount = income.Amount,
                TransactionDate = income.TransactionDate,
                CategoryId = income.CategoryId,
                CategoryName = income.Category?.Name,
                Notes = income.Notes,
                CreatedAt = income.CreatedAt
            };

            return Ok(incomeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching income");
            return StatusCode(500, new { message = "An error occurred while fetching the income" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<IncomeDto>> CreateIncome([FromBody] CreateIncomeRequest request)
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

            var income = new Income
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

            _context.Incomes.Add(income);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Income created for user {userId}");

            var incomeDto = new IncomeDto
            {
                Id = income.Id,
                Description = income.Description,
                Amount = income.Amount,
                TransactionDate = income.TransactionDate,
                CategoryId = income.CategoryId,
                CategoryName = category.Name,
                Notes = income.Notes,
                CreatedAt = income.CreatedAt
            };

            return CreatedAtAction(nameof(GetIncome), new { id = income.Id }, incomeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating income");
            return StatusCode(500, new { message = "An error occurred while creating the income" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncome(int id, [FromBody] UpdateIncomeRequest request)
    {
        try
        {
            var userId = GetUserId();
            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (income == null)
                return NotFound(new { message = "Income not found" });

            if (!string.IsNullOrWhiteSpace(request.Description))
                income.Description = request.Description;

            if (request.Amount.HasValue && request.Amount > 0)
                income.Amount = request.Amount.Value;

            if (request.TransactionDate.HasValue)
                income.TransactionDate = request.TransactionDate.Value;

            if (request.CategoryId.HasValue)
            {
                // Verify new category belongs to user
                var category = await _context.Categories.FirstOrDefaultAsync(
                    c => c.Id == request.CategoryId && c.UserId == userId);

                if (category == null)
                    return BadRequest(new { message = "Invalid category" });

                income.CategoryId = request.CategoryId.Value;
            }

            if (request.Notes != null)
                income.Notes = request.Notes;

            income.UpdatedAt = DateTime.UtcNow;

            _context.Incomes.Update(income);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Income {id} updated for user {userId}");

            return Ok(new { message = "Income updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating income");
            return StatusCode(500, new { message = "An error occurred while updating the income" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncome(int id)
    {
        try
        {
            var userId = GetUserId();
            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (income == null)
                return NotFound(new { message = "Income not found" });

            _context.Incomes.Remove(income);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Income {id} deleted for user {userId}");

            return Ok(new { message = "Income deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting income");
            return StatusCode(500, new { message = "An error occurred while deleting the income" });
        }
    }
}

// DTOs
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
