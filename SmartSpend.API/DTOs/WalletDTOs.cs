using System.ComponentModel.DataAnnotations;

namespace SmartSpend.API.DTOs
{
    public class WalletDto
    {
        public int WalletId { get; set; }
        public string? Name { get; set; }
        public string? WalletType { get; set; }
        public decimal Balance { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencySymbol { get; set; }
    }

    public class CreateWalletDto
    {
        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string? WalletType { get; set; }

        [Required]
        public decimal OpeningBalance { get; set; }

        [Required]
        public int CurrencyId { get; set; }
    }

    public class UpdateWalletDto
    {
        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string? WalletType { get; set; }

        [Required]
        public int CurrencyId { get; set; }
    }

    public class WalletSummaryDto
    {
        public int WalletId { get; set; }
        public string? Name { get; set; }
        public decimal Balance { get; set; }
        public string? WalletType { get; set; }
    }
}
