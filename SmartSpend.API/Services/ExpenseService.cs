using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using SmartSpend.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSpend.API.Services
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseDto>> GetExpensesAsync(int userId, int? walletId, int? categoryId, DateTime? startDate, DateTime? endDate, string? search, string? transactionType);
        Task<ExpenseDto> CreateExpenseAsync(int userId, CreateExpenseDto request);
        Task<ExpenseDto> UpdateExpenseAsync(int userId, int expenseId, UpdateExpenseDto request);
        Task<bool> DeleteExpenseAsync(int userId, int expenseId);
    }

    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static decimal ApplyToWallet(decimal balance, decimal amount, string transactionType, bool reverse = false)
        {
            // Income adds, Expense deducts. If reverse=true, flip the logic (for undo on delete/update)
            bool isIncome = transactionType == "Income";
            if (reverse) isIncome = !isIncome;
            return isIncome ? balance + amount : balance - amount;
        }

        public async Task<IEnumerable<ExpenseDto>> GetExpensesAsync(int userId, int? walletId, int? categoryId, DateTime? startDate, DateTime? endDate, string? search, string? transactionType)
        {
            var query = _context.Expenses
                .Include(e => e.Wallet)
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && !e.IsDeleted);

            if (walletId.HasValue)        query = query.Where(e => e.WalletId == walletId.Value);
            if (categoryId.HasValue)      query = query.Where(e => e.CategoryId == categoryId.Value);
            if (startDate.HasValue)       query = query.Where(e => e.ExpenseDate >= startDate.Value.Date);
            if (endDate.HasValue)         query = query.Where(e => e.ExpenseDate <= endDate.Value.Date);
            if (!string.IsNullOrEmpty(transactionType)) query = query.Where(e => e.TransactionType == transactionType);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(e => e.Description!.Contains(search) || e.Category!.Name!.Contains(search));

            return await query
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new ExpenseDto
                {
                    ExpenseId = e.ExpenseId,
                    Amount = e.Amount,
                    Date = e.ExpenseDate,
                    Description = e.Description,
                    TransactionType = e.TransactionType,
                    WalletId = e.WalletId,
                    WalletName = e.Wallet!.Name,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category!.Name,
                    CategoryIcon = e.Category.Icon,
                    CategoryColor = e.Category.Color
                })
                .ToListAsync();
        }

        public async Task<ExpenseDto> CreateExpenseAsync(int userId, CreateExpenseDto request)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == request.WalletId && w.UserId == userId && w.IsActive);
            if (wallet == null) return null!;

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && (c.UserId == userId || c.IsDefault));
            if (category == null) return null!;

            if (request.TransactionType == "Expense" && wallet.Balance < request.Amount)
            {
                throw new InvalidOperationException($"Insufficient balance in wallet '{wallet.Name}'. Available: {wallet.Balance}, Required: {request.Amount}.");
            }

            var expense = new Expense
            {
                UserId = userId,
                WalletId = request.WalletId,
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                ExpenseDate = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
                Description = request.Description,
                TransactionType = request.TransactionType,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Expenses.Add(expense);

            // Income → add to wallet, Expense → deduct from wallet
            wallet.Balance = ApplyToWallet(wallet.Balance, request.Amount, request.TransactionType);

            await _context.SaveChangesAsync();

            return new ExpenseDto
            {
                ExpenseId = expense.ExpenseId,
                Amount = expense.Amount,
                Date = expense.ExpenseDate,
                Description = expense.Description,
                TransactionType = expense.TransactionType,
                WalletId = expense.WalletId,
                WalletName = wallet.Name,
                CategoryId = expense.CategoryId,
                CategoryName = category.Name,
                CategoryIcon = category.Icon,
                CategoryColor = category.Color
            };
        }

        public async Task<ExpenseDto> UpdateExpenseAsync(int userId, int expenseId, UpdateExpenseDto request)
        {
            var expense = await _context.Expenses
                .Include(e => e.Wallet)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.ExpenseId == expenseId && e.UserId == userId && !e.IsDeleted);

            if (expense == null) return null!;

            var oldWallet = expense.Wallet!;
            var newWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == request.WalletId && w.UserId == userId && w.IsActive);
            if (newWallet == null) return null!;

            var newCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && (c.UserId == userId || c.IsDefault));
            if (newCategory == null) return null!;

            // Calculate hypothetical new balance for the target wallet
            decimal currentTargetBalance = newWallet.WalletId == oldWallet.WalletId ? 
                ApplyToWallet(oldWallet.Balance, expense.Amount, expense.TransactionType, reverse: true) : 
                newWallet.Balance;

            if (request.TransactionType == "Expense" && currentTargetBalance < request.Amount)
            {
                throw new InvalidOperationException($"Insufficient balance in wallet '{newWallet.Name}'. Available: {currentTargetBalance}, Required: {request.Amount}.");
            }

            // Reverse the old transaction from the old wallet
            oldWallet.Balance = ApplyToWallet(oldWallet.Balance, expense.Amount, expense.TransactionType, reverse: true);

            // Apply the new transaction to the new wallet
            newWallet.Balance = ApplyToWallet(newWallet.Balance, request.Amount, request.TransactionType);

            expense.Amount = request.Amount;
            expense.ExpenseDate = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
            expense.CategoryId = request.CategoryId;
            expense.WalletId = request.WalletId;
            expense.Description = request.Description;
            expense.TransactionType = request.TransactionType;

            await _context.SaveChangesAsync();

            return new ExpenseDto
            {
                ExpenseId = expense.ExpenseId,
                Amount = expense.Amount,
                Date = expense.ExpenseDate,
                Description = expense.Description,
                TransactionType = expense.TransactionType,
                WalletId = expense.WalletId,
                WalletName = newWallet.Name,
                CategoryId = expense.CategoryId,
                CategoryName = newCategory.Name,
                CategoryIcon = newCategory.Icon,
                CategoryColor = newCategory.Color
            };
        }

        public async Task<bool> DeleteExpenseAsync(int userId, int expenseId)
        {
            var expense = await _context.Expenses
                .Include(e => e.Wallet)
                .FirstOrDefaultAsync(e => e.ExpenseId == expenseId && e.UserId == userId && !e.IsDeleted);

            if (expense == null) return false;

            expense.IsDeleted = true;

            // Reverse the transaction (refund income or restore expense)
            if (expense.Wallet != null)
                expense.Wallet.Balance = ApplyToWallet(expense.Wallet.Balance, expense.Amount, expense.TransactionType, reverse: true);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
