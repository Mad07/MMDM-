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

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> AccountOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
