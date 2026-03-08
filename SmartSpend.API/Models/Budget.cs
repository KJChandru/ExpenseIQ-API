using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSpend.API.Models
{
    public class Budget
    {
        [Key]
        public int BudgetId { get; set; }

        public int UserId { get; set; }

        public int CategoryId { get; set; }

        [Required]
        [MaxLength(7)]
        public string? MonthYear { get; set; } // e.g., "2025-03"

        [Column(TypeName = "decimal(18,2)")]
        public decimal LimitAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}
