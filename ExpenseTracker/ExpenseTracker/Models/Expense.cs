namespace ExpenseTracker.Models;

public class Expense
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public User? User { get; set; }
    public Category? Category { get; set; }
}
