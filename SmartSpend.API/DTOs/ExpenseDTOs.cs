using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSpend.API.DTOs
{
    public class ExpenseDto
    {
        public int ExpenseId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public string TransactionType { get; set; } = "Expense";
        public int WalletId { get; set; }
        public string? WalletName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryIcon { get; set; }
        public string? CategoryColor { get; set; }
    }

    public class CreateExpenseDto
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int WalletId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [RegularExpression("^(Income|Expense)$", ErrorMessage = "TransactionType must be 'Income' or 'Expense'")]
        public string TransactionType { get; set; } = "Expense";

        [MaxLength(255)]
        public string? Description { get; set; }
    }

    public class UpdateExpenseDto
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int WalletId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [RegularExpression("^(Income|Expense)$", ErrorMessage = "TransactionType must be 'Income' or 'Expense'")]
        public string TransactionType { get; set; } = "Expense";

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}
