namespace ExpenseTracker.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#CCCCCC";
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
}
