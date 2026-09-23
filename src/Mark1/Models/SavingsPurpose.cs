using System.ComponentModel.DataAnnotations;

namespace Mark1.Models
{
    /// <summary>A user-managed tag (e.g. "Emergency Fund", "Vacation") attached to Expenses that
    /// transfer money to Savings or Retained, so those balances can be broken down by purpose.</summary>
    public class SavingsPurpose
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
