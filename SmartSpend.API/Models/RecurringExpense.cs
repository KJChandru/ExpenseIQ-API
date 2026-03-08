using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSpend.API.Models
{
    public class RecurringExpense
    {
        [Key]
        public int RecurringId { get; set; }

        public int UserId { get; set; }

        public int WalletId { get; set; }

        public int CategoryId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "Subscription"; // Subscription | EMI

        [Required]
        [MaxLength(10)]
        public string? Frequency { get; set; } // Daily | Weekly | Monthly

        // EMI Specific Fields
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrincipalAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? InterestRate { get; set; }

        public int? TenureMonths { get; set; }

        public int InstallmentsPaid { get; set; } = 0;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime NextDueDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("WalletId")]
        public virtual Wallet? Wallet { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}
