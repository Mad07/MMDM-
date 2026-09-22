using System.ComponentModel.DataAnnotations.Schema;

namespace Mark1.Models
{
    /// <summary>Per-user settings row: fixed CRC-per-USD rate plus whether to prefer the live rate lookup.</summary>
    public class AppSettings
    {
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal FixedExchangeRate { get; set; } = 525m;

        public bool UseLiveRate { get; set; }

        /// <summary>Drives which currency's figures appear as the primary (larger) value in
        /// dual-currency displays (Home stats, Account Overview totals) and the default currency
        /// pre-selected when adding a new Expense/Income.</summary>
        public Currency PrimaryCurrency { get; set; } = Currency.USD;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
