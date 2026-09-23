using Mark1.Models;

namespace Mark1.Models.ViewModels
{
    public class SettingsViewModel
    {
        public string ActiveTab { get; set; } = "categories";
        public string CurrentUserId { get; set; } = string.Empty;

        public List<Category> Categories { get; set; } = new();
        public List<Account> Accounts { get; set; } = new();
        public List<ApplicationUser> Users { get; set; } = new();
        public List<SavingsPurpose> SavingsPurposes { get; set; } = new();

        public AppSettings CurrencySettings { get; set; } = new();
        public decimal? LiveRatePreview { get; set; }
    }
}
