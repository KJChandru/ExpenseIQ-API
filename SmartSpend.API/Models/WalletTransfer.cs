using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSpend.API.Models
{
    public class WalletTransfer
    {
        [Key]
        public int TransferId { get; set; }

        public int UserId { get; set; }

        public int FromWalletId { get; set; }

        public int ToWalletId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(250)]
        public string? Note { get; set; }

        public DateTime TransferredAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("FromWalletId")]
        public virtual Wallet? FromWallet { get; set; }

        [ForeignKey("ToWalletId")]
        public virtual Wallet? ToWallet { get; set; }
    }
}
