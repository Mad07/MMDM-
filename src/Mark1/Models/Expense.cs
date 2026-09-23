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

        /// <summary>When true, this expense's amount is mirrored as an Income on the user's
        /// Savings account (see TransferIncomeId) - money moving between accounts, not real spending.
        /// Mutually exclusive with IsTransferToRetained (enforced in the form UI and in the
        /// controller, which prioritizes Savings if both somehow arrive true).</summary>
        public bool IsTransferToSavings { get; set; }

        /// <summary>Same idea as IsTransferToSavings, but mirrors to an account named "Retained".</summary>
        public bool IsTransferToRetained { get; set; }

        /// <summary>Id of the auto-generated Income this expense mirrors into Savings, kept in sync
        /// on edit and removed if IsTransferToSavings is unchecked or the expense is deleted.
        /// Loosely coupled on purpose - no EF relationship/FK constraint against Income.</summary>
        public int? TransferIncomeId { get; set; }

        /// <summary>Only meaningful when IsTransferToSavings or IsTransferToRetained is true -
        /// what the transferred money is for, so Account Overview can break the balance down by
        /// purpose. Cleared server-side whenever neither transfer checkbox is set.</summary>
        public int? SavingsPurposeId { get; set; }
        public SavingsPurpose? SavingsPurpose { get; set; }

        public bool RepeatsMonthly { get; set; }

        /// <summary>Set only on the template row when RepeatsMonthly is true; drives when the next occurrence gets generated.</summary>
        public DateTime? NextOccurrenceDate { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
