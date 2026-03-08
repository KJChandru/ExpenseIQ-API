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
    public interface IBudgetService
    {
        Task<IEnumerable<BudgetDto>> GetBudgetsAsync(int userId, string monthYear);
        Task<BudgetDto> CreateBudgetAsync(int userId, CreateBudgetDto request);
        Task<BudgetDto> UpdateBudgetAsync(int userId, int budgetId, UpdateBudgetDto request);
    }

    public class BudgetService : IBudgetService
    {
        private readonly ApplicationDbContext _context;

        public BudgetService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BudgetDto>> GetBudgetsAsync(int userId, string monthYear)
        {
            if (!DateTime.TryParse($"{monthYear}-01", out var startDate))
                return new List<BudgetDto>();

            var endDate = startDate.AddMonths(1).AddDays(-1);

            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId && b.MonthYear == monthYear)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.UserId == userId && !e.IsDeleted && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
                .GroupBy(e => e.CategoryId)
                .Select(g => new { CategoryId = g.Key, Spent = g.Sum(e => e.Amount) })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Spent);

            return budgets.Select(b => new BudgetDto
            {
                BudgetId = b.BudgetId,
                CategoryId = b.CategoryId,
                CategoryName = b.Category!.Name,
                CategoryColor = b.Category.Color,
                MonthYear = b.MonthYear,
                LimitAmount = b.LimitAmount,
                SpentAmount = expenses.ContainsKey(b.CategoryId) ? expenses[b.CategoryId] : 0
            }).OrderByDescending(b => b.LimitAmount).ToList();
        }

        public async Task<BudgetDto> CreateBudgetAsync(int userId, CreateBudgetDto request)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && (c.UserId == userId || c.IsDefault));
            if (category == null) return null!;

            var existing = await _context.Budgets.FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == request.CategoryId && b.MonthYear == request.MonthYear);
            if (existing != null) return null!;

            var budget = new Budget
            {
                UserId = userId,
                CategoryId = request.CategoryId,
                MonthYear = request.MonthYear!,
                LimitAmount = request.LimitAmount
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            return new BudgetDto
            {
                BudgetId = budget.BudgetId,
                CategoryId = budget.CategoryId,
                CategoryName = category.Name,
                CategoryColor = category.Color,
                MonthYear = budget.MonthYear,
                LimitAmount = budget.LimitAmount,
                SpentAmount = 0
            };
        }

        public async Task<BudgetDto> UpdateBudgetAsync(int userId, int budgetId, UpdateBudgetDto request)
        {
            var budget = await _context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.BudgetId == budgetId && b.UserId == userId);

            if (budget == null) return null!;

            budget.LimitAmount = request.LimitAmount;
            await _context.SaveChangesAsync();

            return new BudgetDto
            {
                BudgetId = budget.BudgetId,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category!.Name,
                CategoryColor = budget.Category.Color,
                MonthYear = budget.MonthYear,
                LimitAmount = budget.LimitAmount,
                SpentAmount = 0
            };
        }
    }
}
