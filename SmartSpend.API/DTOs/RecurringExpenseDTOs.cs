using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSpend.API.DTOs
{
    public class RecurringExpenseDto
    {
        public int RecurringId { get; set; }
        public int WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = "Subscription";
        public decimal? PrincipalAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public int? TenureMonths { get; set; }
        public int InstallmentsPaid { get; set; }
        public string Frequency { get; set; } = string.Empty; // Daily, Weekly, Monthly
        public DateTime StartDate { get; set; }
        public DateTime NextDueDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateRecurringExpenseDto
    {
        [Required]
        public int WalletId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }
        [Required]
        [RegularExpression("^(Subscription|EMI)$", ErrorMessage = "Type must be Subscription or EMI.")]
        public string Type { get; set; } = "Subscription";

        [Range(0.01, double.MaxValue, ErrorMessage = "Principal amount must be greater than zero.")]
        public decimal? PrincipalAmount { get; set; }

        [Range(0.01, 100, ErrorMessage = "Interest rate must be between 0.01 and 100.")]
        public decimal? InterestRate { get; set; }

        [Range(1, 480, ErrorMessage = "Tenure must be between 1 and 480 months.")]
        public int? TenureMonths { get; set; }

        [Required]
        [RegularExpression("^(Daily|Weekly|Monthly)$", ErrorMessage = "Frequency must be Daily, Weekly, or Monthly.")]
        public string Frequency { get; set; } = "Monthly";

        [Required]
        public DateTime StartDate { get; set; }
    }

    public class UpdateRecurringExpenseDto
    {
        [Required]
        public int WalletId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }
        [Required]
        [RegularExpression("^(Subscription|EMI)$", ErrorMessage = "Type must be Subscription or EMI.")]
        public string Type { get; set; } = "Subscription";

        [Range(0.01, double.MaxValue, ErrorMessage = "Principal amount must be greater than zero.")]
        public decimal? PrincipalAmount { get; set; }

        [Range(0.01, 100, ErrorMessage = "Interest rate must be between 0.01 and 100.")]
        public decimal? InterestRate { get; set; }

        [Range(1, 480, ErrorMessage = "Tenure must be between 1 and 480 months.")]
        public int? TenureMonths { get; set; }

        [Required]
        [RegularExpression("^(Daily|Weekly|Monthly)$", ErrorMessage = "Frequency must be Daily, Weekly, or Monthly.")]
        public string Frequency { get; set; } = "Monthly";

        public bool IsActive { get; set; }
    }
}
