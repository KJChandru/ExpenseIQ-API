using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSpend.API.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        public int? UserId { get; set; } // null = system category

        [Required]
        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
