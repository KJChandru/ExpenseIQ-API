using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using SmartSpend.API.Models;

namespace SmartSpend.API.Services
{
    public class RecurringExpenseService : IRecurringExpenseService
    {
        private readonly ApplicationDbContext _context;

        public RecurringExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(int userId)
        {
            return await _context.RecurringExpenses
                .Include(r => r.Wallet)
                .Include(r => r.Category)
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.NextDueDate)
                .Select(r => new RecurringExpenseDto
                {
                    RecurringId = r.RecurringId,
                    WalletId = r.WalletId,
                    WalletName = r.Wallet != null ? r.Wallet.Name : string.Empty,
                    CategoryId = r.CategoryId,
                    CategoryName = r.Category != null ? r.Category.Name : string.Empty,
                    CategoryIcon = r.Category != null ? r.Category.Icon : string.Empty,
                    CategoryColor = r.Category != null ? r.Category.Color : string.Empty,
                    Type = r.Type,
                    PrincipalAmount = r.PrincipalAmount,
                    InterestRate = r.InterestRate,
                    TenureMonths = r.TenureMonths,
                    InstallmentsPaid = r.InstallmentsPaid,
                    Amount = r.Amount,
                    Description = r.Description,
                    Frequency = r.Frequency ?? "Monthly",
                    StartDate = r.StartDate,
                    NextDueDate = r.NextDueDate,
                    IsActive = r.IsActive
                }).ToListAsync();
        }

        public async Task<RecurringExpenseDto> GetRecurringExpenseByIdAsync(int userId, int recurringId)
        {
            var r = await _context.RecurringExpenses
                .Include(re => re.Wallet)
                .Include(re => re.Category)
                .FirstOrDefaultAsync(re => re.UserId == userId && re.RecurringId == recurringId);

            if (r == null) return null;

            return new RecurringExpenseDto
            {
                RecurringId = r.RecurringId,
                WalletId = r.WalletId,
                WalletName = r.Wallet != null ? r.Wallet.Name : string.Empty,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : string.Empty,
                CategoryIcon = r.Category != null ? r.Category.Icon : string.Empty,
                CategoryColor = r.Category != null ? r.Category.Color : string.Empty,
                Type = r.Type,
                PrincipalAmount = r.PrincipalAmount,
                InterestRate = r.InterestRate,
                TenureMonths = r.TenureMonths,
                InstallmentsPaid = r.InstallmentsPaid,
                Amount = r.Amount,
                Description = r.Description,
                Frequency = r.Frequency ?? "Monthly",
                StartDate = r.StartDate,
                NextDueDate = r.NextDueDate,
                IsActive = r.IsActive
            };
        }

        public async Task<RecurringExpenseDto> CreateRecurringExpenseAsync(int userId, CreateRecurringExpenseDto dto)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == dto.WalletId && w.UserId == userId);
            if (wallet == null) throw new ArgumentException("Wallet not found.");

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && (c.UserId == userId || c.UserId == null));
            if (category == null) throw new ArgumentException("Category not found.");

            // Calculate next due date
            DateTime nextDue = dto.StartDate;
            if (dto.StartDate < DateTime.UtcNow.Date)
            {
                // If start date is in the past, align next due date
                while (nextDue < DateTime.UtcNow.Date)
                {
                    nextDue = CalculateNextDueDate(nextDue, dto.Frequency);
                }
            }

            decimal finalAmount = dto.Amount;
            if (dto.Type == "EMI" && dto.PrincipalAmount.HasValue && dto.InterestRate.HasValue && dto.TenureMonths.HasValue)
            {
                // EMI = P * r * (1+r)^n / ((1+r)^n - 1)
                double p = (double)dto.PrincipalAmount.Value;
                double r = (double)dto.InterestRate.Value / 12 / 100;
                int n = dto.TenureMonths.Value;
                if (r > 0)
                {
                    finalAmount = (decimal)(p * r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1));
                }
                else
                {
                    finalAmount = dto.PrincipalAmount.Value / n;
                }
            }

            var recurring = new RecurringExpense
            {
                UserId = userId,
                WalletId = dto.WalletId,
                CategoryId = dto.CategoryId,
                Type = dto.Type,
                PrincipalAmount = dto.PrincipalAmount,
                InterestRate = dto.InterestRate,
                TenureMonths = dto.TenureMonths,
                InstallmentsPaid = 0,
                Amount = finalAmount,
                Description = dto.Description,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
                NextDueDate = nextDue,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecurringExpenses.Add(recurring);
            await _context.SaveChangesAsync();

            return await GetRecurringExpenseByIdAsync(userId, recurring.RecurringId);
        }

        public async Task<RecurringExpenseDto> UpdateRecurringExpenseAsync(int userId, int recurringId, UpdateRecurringExpenseDto dto)
        {
            var recurring = await _context.RecurringExpenses.FirstOrDefaultAsync(r => r.UserId == userId && r.RecurringId == recurringId);
            if (recurring == null) return null!;

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == dto.WalletId && w.UserId == userId);
            if (wallet == null) throw new ArgumentException("Wallet not found.");

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && (c.UserId == userId || c.UserId == null));
            if (category == null) throw new ArgumentException("Category not found.");

            bool frequencyChanged = recurring.Frequency != dto.Frequency;

            decimal finalAmount = dto.Amount;
            if (dto.Type == "EMI" && dto.PrincipalAmount.HasValue && dto.InterestRate.HasValue && dto.TenureMonths.HasValue)
            {
                double p = (double)dto.PrincipalAmount.Value;
                double r = (double)dto.InterestRate.Value / 12 / 100;
                int n = dto.TenureMonths.Value;
                if (r > 0)
                {
                    finalAmount = (decimal)(p * r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1));
                }
                else
                {
                    finalAmount = dto.PrincipalAmount.Value / n;
                }
            }

            recurring.WalletId = dto.WalletId;
            recurring.CategoryId = dto.CategoryId;
            recurring.Type = dto.Type;
            recurring.PrincipalAmount = dto.PrincipalAmount;
            recurring.InterestRate = dto.InterestRate;
            recurring.TenureMonths = dto.TenureMonths;
            recurring.Amount = finalAmount;
            recurring.Description = dto.Description;
            recurring.Frequency = dto.Frequency;
            recurring.IsActive = dto.IsActive;
            recurring.UpdatedAt = DateTime.UtcNow;

            if (frequencyChanged && recurring.IsActive)
            {
                // Re-evaluate next due date if frequency changed
                if (recurring.NextDueDate < DateTime.UtcNow.Date)
                {
                    DateTime nextDue = recurring.NextDueDate;
                    while (nextDue < DateTime.UtcNow.Date)
                    {
                        nextDue = CalculateNextDueDate(nextDue, dto.Frequency);
                    }
                    recurring.NextDueDate = nextDue;
                }
            }

            await _context.SaveChangesAsync();

            return await GetRecurringExpenseByIdAsync(userId, recurring.RecurringId);
        }

        public async Task<bool> DeleteRecurringExpenseAsync(int userId, int recurringId)
        {
            var recurring = await _context.RecurringExpenses.FirstOrDefaultAsync(r => r.UserId == userId && r.RecurringId == recurringId);
            if (recurring == null) return false;

            _context.RecurringExpenses.Remove(recurring);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ProcessDueExpensesAsync(int userId)
        {
            // Find all active recurring expenses for the user where the next due date is today or earlier
            var today = DateTime.UtcNow.Date;
            var dueExpenses = await _context.RecurringExpenses
                .Where(r => r.UserId == userId && r.IsActive && r.NextDueDate.Date <= today)
                .ToListAsync();

            if (!dueExpenses.Any()) return 0;

            int processedCount = 0;

            foreach (var r in dueExpenses)
            {
                int maxIterations = 50; // Safety break to prevent infinite loops for old dates
                int iterations = 0;

                while (r.NextDueDate.Date <= today && r.IsActive && iterations < maxIterations)
                {
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == r.WalletId);
                    if (wallet == null) break;

                    if (wallet.Balance < r.Amount)
                    {
                        throw new InvalidOperationException($"Insufficient balance in wallet '{wallet.Name}' for recurring expense '{r.Description ?? r.Category?.Name}'. Due: {r.Amount}. Wallet Balance: {wallet.Balance}. Processing stopped.");
                    }

                    // Create actual expense entry
                    var newExpense = new Expense
                    {
                        UserId = r.UserId,
                        WalletId = r.WalletId,
                        CategoryId = r.CategoryId,
                        Amount = r.Amount,
                        ExpenseDate = r.NextDueDate.Date, // Record it on the due date
                        Description = string.IsNullOrEmpty(r.Description) ? "Recurring Expense" : r.Description,
                        TransactionType = "Expense", // Recurring expenses are assumed to be expenses
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Expenses.Add(newExpense);

                    // Deduct from wallet
                    wallet.Balance -= r.Amount;

                    // Advance next due date and increment installments if EMI
                    r.NextDueDate = CalculateNextDueDate(r.NextDueDate, r.Frequency ?? "Monthly");
                    
                    if (r.Type == "EMI")
                    {
                        r.InstallmentsPaid++;
                        if (r.TenureMonths.HasValue && r.InstallmentsPaid >= r.TenureMonths.Value)
                        {
                            r.IsActive = false; // Auto-complete the EMI
                        }
                    }

                    r.UpdatedAt = DateTime.UtcNow;

                    processedCount++;
                    iterations++;
                }
            }

            await _context.SaveChangesAsync();
            return processedCount;
        }

        private DateTime CalculateNextDueDate(DateTime currentDue, string frequency)
        {
            return frequency switch
            {
                "Daily" => currentDue.AddDays(1),
                "Weekly" => currentDue.AddDays(7),
                "Monthly" => currentDue.AddMonths(1),
                _ => currentDue.AddMonths(1)
            };
        }
    }
}
