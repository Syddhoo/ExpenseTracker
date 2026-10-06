using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data;

public class ExpenseTrackerContext(DbContextOptions<ExpenseTrackerContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Income> Incomes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Category configuration
        modelBuilder.Entity<Category>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Expense configuration
        modelBuilder.Entity<Expense>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.User)
            .WithMany(u => u.Expenses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Income configuration
        modelBuilder.Entity<Income>()
            .HasKey(i => i.Id);

        modelBuilder.Entity<Income>()
            .HasOne(i => i.User)
            .WithMany(u => u.Incomes)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Income>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Incomes)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed default categories
        SeedDefaultCategories(modelBuilder);
    }

    private void SeedDefaultCategories(ModelBuilder modelBuilder)
    {
        // Note: Categories are user-specific, so we'll seed them per user if needed
        // For now, this is intentionally empty as categories will be created per user
    }
}
