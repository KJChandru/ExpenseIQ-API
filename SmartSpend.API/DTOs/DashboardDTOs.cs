using System.Collections.Generic;

namespace SmartSpend.API.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TotalBalance { get; set; }
        public decimal MonthlySpend { get; set; }
        public string? TopCategory { get; set; }
        public IEnumerable<WalletSummaryDto>? Wallets { get; set; }
    }

    public class MonthlyTrendDto
    {
        public string? Month { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class CategoryBreakdownDto
    {
        public string? CategoryName { get; set; }
        public decimal TotalSpent { get; set; }
        public string? Color { get; set; }
    }
}
