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

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
