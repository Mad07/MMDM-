using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mark1.Models.ViewModels
{
    public class TransactionFilterViewModel<TItem>
    {
        public List<TItem> Items { get; set; } = new();
        public decimal TotalUsd { get; set; }
        public decimal TotalCrc { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? CategoryId { get; set; }
        public int? AccountId { get; set; }
        public Currency? Currency { get; set; }

        /// <summary>Selected quick-filter month from the 6-month tab strip, "yyyy-MM", or null/"all".</summary>
        public string? Month { get; set; }
        public List<(string Value, string Label)> MonthTabs { get; set; } = new();

        public string SortBy { get; set; } = "date";
        public bool SortDescending { get; set; } = true;

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> AccountOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
