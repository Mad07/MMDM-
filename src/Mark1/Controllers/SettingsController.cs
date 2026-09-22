using Mark1.Data;
using Mark1.Models;
using Mark1.Models.ViewModels;
using Mark1.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mark1.Controllers
{
    public class SettingsController : AuthorizedController
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(ApplicationDbContext db, ICurrencyService currencyService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _currencyService = currencyService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string tab = "categories")
        {
            var settings = await GetOrCreateSettingsAsync();

            var vm = new SettingsViewModel
            {
                ActiveTab = tab,
                CurrentUserId = CurrentUserId,
                Categories = await _db.Categories.Where(c => c.UserId == CurrentUserId).OrderBy(c => c.Name).ToListAsync(),
                Accounts = await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync(),
                Users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync(),
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
        public async Task<IActionResult> CreateUser(string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["SettingsError"] = "Email and password are required.";
                return RedirectToAction(nameof(Index), new { tab = "users" });
            }

            if (password != confirmPassword)
            {
                TempData["SettingsError"] = "Passwords don't match.";
                return RedirectToAction(nameof(Index), new { tab = "users" });
            }

            var user = new ApplicationUser { UserName = email.Trim(), Email = email.Trim(), EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                TempData["SettingsError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index), new { tab = "users" });
            }

            await IdentitySeeder.SeedDefaultDataAsync(_db, user.Id);
            return RedirectToAction(nameof(Index), new { tab = "users" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (id == CurrentUserId)
            {
                TempData["SettingsError"] = "You can't delete the account you're currently logged in as.";
                return RedirectToAction(nameof(Index), new { tab = "users" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index), new { tab = "users" });
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
