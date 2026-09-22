using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mark1.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public Currency Currency { get; set; } = Currency.USD;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int AccountId { get; set; }
        public Account? Account { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>Whether this expense has actually been paid out. Unpaid expenses are excluded
        /// from the Account Overview balances until checked off.</summary>
        public bool IsPaid { get; set; }

        public bool RepeatsMonthly { get; set; }

        /// <summary>Set only on the template row when RepeatsMonthly is true; drives when the next occurrence gets generated.</summary>
        public DateTime? NextOccurrenceDate { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
