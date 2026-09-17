using Mark1.Models;

namespace Mark1.Models.ViewModels
{
    public class SettingsViewModel
    {
        public string ActiveTab { get; set; } = "categories";

        public List<Category> Categories { get; set; } = new();
        public List<Account> Accounts { get; set; } = new();

        public AppSettings CurrencySettings { get; set; } = new();
        public decimal? LiveRatePreview { get; set; }
    }
}
