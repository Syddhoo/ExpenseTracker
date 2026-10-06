using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseTracker.Data;

public class ExpenseTrackerContextFactory : IDesignTimeDbContextFactory<ExpenseTrackerContext>
{
    public ExpenseTrackerContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExpenseTrackerContext>();

        // Use SQLite database file
        var databasePath = Path.Combine(AppContext.BaseDirectory, "expensetracker.db");
        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new ExpenseTrackerContext(optionsBuilder.Options);
    }
}
