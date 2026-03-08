using System.ComponentModel.DataAnnotations;

namespace SmartSpend.API.DTOs
{
    public class BudgetDto
    {
        public int BudgetId { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public string? MonthYear { get; set; }
        public decimal LimitAmount { get; set; }
        public decimal SpentAmount { get; set; }
    }

    public class CreateBudgetDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        [RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "Format must be YYYY-MM")]
        public string? MonthYear { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal LimitAmount { get; set; }
    }

    public class UpdateBudgetDto
    {
        [Required]
        [Range(0, double.MaxValue)]
        public decimal LimitAmount { get; set; }
    }
}
