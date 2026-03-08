using System.ComponentModel.DataAnnotations;

namespace SmartSpend.API.DTOs
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }
    }

    public class UpdateCategoryDto
    {
        [Required]
        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }
    }
}
