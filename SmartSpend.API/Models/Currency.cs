using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSpend.API.Models
{
    public class Currency
    {
        [Key]
        public int CurrencyId { get; set; }

        [Required]
        [MaxLength(5)]
        public string? Code { get; set; } 

        [Required]
        [MaxLength(50)]
        public string? Name { get; set; } 

        [Required]
        [MaxLength(5)]
        public string? Symbol { get; set; } 

        [MaxLength(10)]
        public string? Flag { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
