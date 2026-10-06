namespace ExpenseTracker.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
