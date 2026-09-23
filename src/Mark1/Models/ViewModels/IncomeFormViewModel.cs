using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mark1.Models.ViewModels
{
    public class IncomeFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public Currency Currency { get; set; } = Currency.USD;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int AccountId { get; set; }

        public bool RepeatsMonthly { get; set; }

        /// <summary>Only meaningful when AccountId is the user's "Savings" or "Retained" account;
        /// hidden/cleared client-side otherwise, and cleared server-side as a fallback.</summary>
        public int? SavingsPurposeId { get; set; }

        /// <summary>Where to redirect after saving - carries forward whatever filtered/sorted
        /// Incomes view the user came from, instead of always resetting to the unfiltered list.</summary>
        public string? ReturnUrl { get; set; }

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> AccountOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> SavingsPurposeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        /// <summary>Lets the view/JS know which AccountOptions values should reveal the purpose
        /// dropdown, without hardcoding literal account names client-side.</summary>
        public int? SavingsAccountId { get; set; }
        public int? RetainedAccountId { get; set; }
    }
}
