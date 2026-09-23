using Mark1.Models;

namespace Mark1.Models.ViewModels
{
    public class AccountCardViewModel
    {
        public string AccountName { get; set; } = string.Empty;
        public decimal NetUsd { get; set; }
        public decimal NetCrc { get; set; }
        public decimal NetMixedUsd { get; set; }
        public bool HasUsdActivity { get; set; }
        public bool HasCrcActivity { get; set; }

        /// <summary>Only populated for accounts literally named "Savings" or "Retained" - how much
        /// of the paid transfers into this account break down by purpose tag.</summary>
        public List<SavingsPurposeBreakdownItem> PurposeBreakdown { get; set; } = new();
    }

    public class SavingsPurposeBreakdownItem
    {
        public string PurposeName { get; set; } = string.Empty;
        public decimal AmountUsd { get; set; }
        public decimal AmountCrc { get; set; }
    }

    public class AccountOverviewViewModel
    {
        public Currency PrimaryCurrency { get; set; } = Currency.USD;

        public string SelectedPeriod { get; set; } = "all";
        public List<(string Value, string Label)> PeriodOptions { get; set; } = new();

        public decimal GrandTotalUsdOnly { get; set; }
        public decimal GrandTotalCrcOnly { get; set; }
        public decimal GrandTotalUsdMixed { get; set; }
        public decimal GrandTotalCrcMixed { get; set; }

        public List<AccountCardViewModel> AccountCards { get; set; } = new();
    }
}
