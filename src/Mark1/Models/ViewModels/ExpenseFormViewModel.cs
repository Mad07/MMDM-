using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mark1.Models.ViewModels
{
    public class ExpenseFormViewModel
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

        public bool IsPaid { get; set; }

        public bool IsTransferToSavings { get; set; }

        public bool IsTransferToRetained { get; set; }

        /// <summary>Only meaningful when IsTransferToSavings or IsTransferToRetained is checked;
        /// hidden/cleared client-side otherwise, and cleared server-side as a fallback.</summary>
        public int? SavingsPurposeId { get; set; }

        /// <summary>Where to redirect after saving - carries forward whatever filtered/sorted
        /// Expenses view the user came from, instead of always resetting to the unfiltered list.</summary>
        public string? ReturnUrl { get; set; }

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> AccountOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> SavingsPurposeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
