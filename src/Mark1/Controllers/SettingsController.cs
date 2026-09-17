using Mark1.Data;
using Mark1.Models;
using Mark1.Models.ViewModels;
using Mark1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mark1.Controllers
{
    public class SettingsController : AuthorizedController
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrencyService _currencyService;

        public SettingsController(ApplicationDbContext db, ICurrencyService currencyService)
        {
            _db = db;
            _currencyService = currencyService;
        }

        public async Task<IActionResult> Index(string tab = "categories")
        {
            var settings = await GetOrCreateSettingsAsync();

            var vm = new SettingsViewModel
            {
                ActiveTab = tab,
                Categories = await _db.Categories.Where(c => c.UserId == CurrentUserId).OrderBy(c => c.Name).ToListAsync(),
                Accounts = await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync(),
                CurrencySettings = settings
            };

            if (settings.UseLiveRate)
            {
                vm.LiveRatePreview = await _currencyService.GetUsdToCrcRateAsync(settings.FixedExchangeRate, true);
            }

            return View(vm);
        }

        private async Task<AppSettings> GetOrCreateSettingsAsync()
        {
            var settings = await _db.AppSettings.FirstOrDefaultAsync(s => s.UserId == CurrentUserId);
            if (settings == null)
            {
                settings = new AppSettings { UserId = CurrentUserId, FixedExchangeRate = 525m, UseLiveRate = true };
                _db.AppSettings.Add(settings);
                await _db.SaveChangesAsync();
            }
            return settings;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _db.Categories.Add(new Category { Name = name.Trim(), UserId = CurrentUserId });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (category != null)
            {
                var inUse = await _db.Expenses.AnyAsync(e => e.CategoryId == id) || await _db.Incomes.AnyAsync(i => i.CategoryId == id);
                if (!inUse)
                {
                    _db.Categories.Remove(category);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    TempData["SettingsError"] = "Can't delete a category that's used by existing transactions.";
                }
            }
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccount(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _db.Accounts.Add(new Account { Name = name.Trim(), UserId = CurrentUserId });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { tab = "accounts" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == CurrentUserId);
            if (account != null)
            {
                var inUse = await _db.Expenses.AnyAsync(e => e.AccountId == id) || await _db.Incomes.AnyAsync(i => i.AccountId == id);
                if (!inUse)
                {
                    _db.Accounts.Remove(account);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    TempData["SettingsError"] = "Can't delete an account that's used by existing transactions.";
                }
            }
            return RedirectToAction(nameof(Index), new { tab = "accounts" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCurrency(decimal fixedExchangeRate, bool useLiveRate)
        {
            var settings = await GetOrCreateSettingsAsync();
            settings.FixedExchangeRate = fixedExchangeRate;
            settings.UseLiveRate = useLiveRate;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { tab = "currency" });
        }
    }
}
