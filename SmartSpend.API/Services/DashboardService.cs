using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSpend.API.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(int userId);
        Task<IEnumerable<MonthlyTrendDto>> GetMonthlyTrendAsync(int userId);
        Task<IEnumerable<CategoryBreakdownDto>> GetCategoryBreakdownAsync(int userId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId && w.IsActive)
                .Select(w => new WalletSummaryDto
                {
                    WalletId = w.WalletId,
                    Name = w.Name,
                    Balance = w.Balance,
                    WalletType = w.WalletType
                })
                .ToListAsync();

            var totalBalance = wallets.Sum(w => w.Balance);

            var currentMonthExpenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && !e.IsDeleted && e.ExpenseDate >= startOfMonth)
                .ToListAsync();

            var monthlySpend = currentMonthExpenses.Sum(e => e.Amount);

            var topCategory = currentMonthExpenses
                .GroupBy(e => e.Category?.Name ?? "Uncategorized")
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            return new DashboardSummaryDto
            {
                TotalBalance = totalBalance,
                MonthlySpend = monthlySpend,
                TopCategory = topCategory?.Category ?? "N/A",
                Wallets = wallets
            };
        }

        public async Task<IEnumerable<MonthlyTrendDto>> GetMonthlyTrendAsync(int userId)
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var expenses = await _context.Expenses
                .Where(e => e.UserId == userId && !e.IsDeleted && e.ExpenseDate >= startDate)
                .ToListAsync();

            var trend = expenses
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalSpent = g.Sum(e => e.Amount)
                })
                .OrderBy(x => DateTime.ParseExact(x.Month!, "MMM yyyy", CultureInfo.InvariantCulture))
                .ToList();

            var result = new List<MonthlyTrendDto>();
            for (int i = 0; i < 6; i++)
            {
                var monthLabel = startDate.AddMonths(i).ToString("MMM yyyy");
                var existing = trend.FirstOrDefault(t => t.Month == monthLabel);
                result.Add(existing ?? new MonthlyTrendDto { Month = monthLabel, TotalSpent = 0 });
            }

            return result;
        }

        public async Task<IEnumerable<CategoryBreakdownDto>> GetCategoryBreakdownAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            return await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && !e.IsDeleted && e.ExpenseDate >= startOfMonth)
                .GroupBy(e => new { e.Category!.Name, e.Category.Color })
                .Select(g => new CategoryBreakdownDto
                {
                    CategoryName = g.Key.Name,
                    Color = g.Key.Color,
                    TotalSpent = g.Sum(e => e.Amount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToListAsync();
        }
    }
}
