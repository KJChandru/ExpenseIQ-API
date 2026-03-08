using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Models;

namespace SmartSpend.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<WalletTransfer> WalletTransfers { get; set; }
        public DbSet<RecurringExpense> RecurringExpenses { get; set; }
        public DbSet<Budget> Budgets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints to match Phase 1 SQL Script

            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                // CK for WalletType is typically handled at application level or manually added via Migration SQL
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.ExpenseDate }).IsDescending(false, true); // From Phase 1 IX_Expenses_UserId_Date
                entity.HasIndex(e => e.WalletId);
            });

            modelBuilder.Entity<WalletTransfer>(entity =>
            {
                entity.HasOne(d => d.FromWallet)
                      .WithMany()
                      .HasForeignKey(d => d.FromWalletId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.ToWallet)
                      .WithMany()
                      .HasForeignKey(d => d.ToWalletId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<RecurringExpense>(entity =>
            {
                entity.HasIndex(e => new { e.NextDueDate, e.IsActive });
            });

            modelBuilder.Entity<Budget>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.MonthYear });
                entity.HasIndex(e => new { e.UserId, e.CategoryId, e.MonthYear }).IsUnique();
            });
        }
    }
}
