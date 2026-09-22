using Mark1.Data;
using Mark1.Helpers;
using Mark1.Models;
using Mark1.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Mark1.Controllers
{
    public class ExpensesController : AuthorizedController
    {
        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ExpensesController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int? categoryId, int? accountId, Currency? currency, string? month)
        {
            var query = _db.Expenses
                .Include(e => e.Category)
                .Include(e => e.Account)
                .Where(e => e.UserId == CurrentUserId && !e.IsDeleted);

            if (!string.IsNullOrEmpty(month) && month != "all" && DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var monthStart))
            {
                var monthEnd = monthStart.AddMonths(1);
                query = query.Where(e => e.Date >= monthStart && e.Date < monthEnd);
            }
            else
            {
                if (dateFrom.HasValue) query = query.Where(e => e.Date >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(e => e.Date <= dateTo.Value);
            }

            if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);
            if (accountId.HasValue) query = query.Where(e => e.AccountId == accountId.Value);
            if (currency.HasValue) query = query.Where(e => e.Currency == currency.Value);

            var items = await query.OrderByDescending(e => e.Date).ToListAsync();

            var vm = new TransactionFilterViewModel<Expense>
            {
                Items = items,
                TotalUsd = items.Where(e => e.Currency == Currency.USD).Sum(e => e.Amount),
                TotalCrc = items.Where(e => e.Currency == Currency.CRC).Sum(e => e.Amount),
                DateFrom = dateFrom,
                DateTo = dateTo,
                CategoryId = categoryId,
                AccountId = accountId,
                Currency = currency,
                Month = string.IsNullOrEmpty(month) ? "all" : month,
                MonthTabs = BuildMonthTabs(_localizer),
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Deleted()
        {
            var items = await _db.Expenses
                .Include(e => e.Category)
                .Include(e => e.Account)
                .Where(e => e.UserId == CurrentUserId && e.IsDeleted)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense != null)
            {
                expense.IsDeleted = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Deleted));
        }

        public async Task<IActionResult> Create()
        {
            var vm = new ExpenseFormViewModel
            {
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpenseFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryOptions = await GetCategoryOptionsAsync();
                model.AccountOptions = await GetAccountOptionsAsync();
                return View(model);
            }

            var expense = new Expense
            {
                Description = model.Description,
                Amount = model.Amount,
                Currency = model.Currency,
                Date = model.Date,
                CategoryId = model.CategoryId,
                AccountId = model.AccountId,
                RepeatsMonthly = model.RepeatsMonthly,
                NextOccurrenceDate = model.RepeatsMonthly ? model.Date.AddMonths(1) : null,
                IsPaid = model.IsPaid,
                IsTransferToSavings = model.IsTransferToSavings,
                UserId = CurrentUserId
            };
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            await SyncTransferIncomeAsync(expense, model.IsTransferToSavings);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense == null) return NotFound();

            var vm = new ExpenseFormViewModel
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                Currency = expense.Currency,
                Date = expense.Date,
                CategoryId = expense.CategoryId,
                AccountId = expense.AccountId,
                RepeatsMonthly = expense.RepeatsMonthly,
                IsPaid = expense.IsPaid,
                IsTransferToSavings = expense.IsTransferToSavings,
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExpenseFormViewModel model)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.CategoryOptions = await GetCategoryOptionsAsync();
                model.AccountOptions = await GetAccountOptionsAsync();
                return View(model);
            }

            expense.Description = model.Description;
            expense.Amount = model.Amount;
            expense.Currency = model.Currency;
            expense.Date = model.Date;
            expense.CategoryId = model.CategoryId;
            expense.AccountId = model.AccountId;
            expense.IsPaid = model.IsPaid;

            if (model.RepeatsMonthly && !expense.RepeatsMonthly)
            {
                expense.NextOccurrenceDate = model.Date.AddMonths(1);
            }
            else if (!model.RepeatsMonthly)
            {
                expense.NextOccurrenceDate = null;
            }
            expense.RepeatsMonthly = model.RepeatsMonthly;
            expense.IsTransferToSavings = model.IsTransferToSavings;

            await SyncTransferIncomeAsync(expense, model.IsTransferToSavings);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePaid(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense != null)
            {
                expense.IsPaid = !expense.IsPaid;
                await _db.SaveChangesAsync();
            }
            return RedirectToReferrer();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense != null)
            {
                expense.IsDeleted = true;
                // The linked transfer income is removed rather than kept dangling - if the expense
                // is later restored, the transfer link is gone and the checkbox reverts to unchecked.
                await SyncTransferIncomeAsync(expense, wantTransfer: false);
                expense.IsTransferToSavings = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Sends the user back to whichever filtered/paged Expenses view they toggled
        /// Paid from, instead of always resetting to the unfiltered list.</summary>
        private IActionResult RedirectToReferrer()
        {
            var referer = Request.Headers.Referer.ToString();
            if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) && refererUri.Host == Request.Host.Host)
            {
                return Redirect(refererUri.PathAndQuery);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Keeps an expense's mirrored Savings-account Income in sync: creates one if
        /// wantTransfer is newly true, updates it in place if the expense's amount/date/etc.
        /// changed, or removes it if wantTransfer is false. No-ops if the user has no account
        /// literally named "Savings" (renamed/deleted) - silently leaves the expense as a plain
        /// expense in that case rather than failing the save.</summary>
        private async Task SyncTransferIncomeAsync(Expense expense, bool wantTransfer)
        {
            if (!wantTransfer)
            {
                if (expense.TransferIncomeId.HasValue)
                {
                    var existing = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == expense.TransferIncomeId.Value && i.UserId == CurrentUserId);
                    if (existing != null) _db.Incomes.Remove(existing);
                    expense.TransferIncomeId = null;
                }
                return;
            }

            var savingsAccount = await GetSavingsAccountAsync();
            if (savingsAccount == null)
            {
                TempData["ExpenseTransferWarning"] = _localizer["Expenses_TransferNoSavingsAccount"].Value;
                expense.TransferIncomeId = null;
                return;
            }

            var transferCategory = await GetOrCreateTransferCategoryAsync();

            var income = expense.TransferIncomeId.HasValue
                ? await _db.Incomes.FirstOrDefaultAsync(i => i.Id == expense.TransferIncomeId.Value && i.UserId == CurrentUserId)
                : null;

            if (income == null)
            {
                income = new Income { UserId = CurrentUserId };
                _db.Incomes.Add(income);
            }

            income.Description = _localizer["Expenses_TransferIncomeDescription", expense.Description];
            income.Amount = expense.Amount;
            income.Currency = expense.Currency;
            income.Date = expense.Date;
            income.CategoryId = transferCategory.Id;
            income.AccountId = savingsAccount.Id;

            await _db.SaveChangesAsync();
            expense.TransferIncomeId = income.Id;
        }

        private async Task<Account?> GetSavingsAccountAsync() =>
            await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == CurrentUserId && a.Name == "Savings");

        private async Task<Category> GetOrCreateTransferCategoryAsync()
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.Name == "Transfer");
            if (category == null)
            {
                category = new Category { Name = "Transfer", UserId = CurrentUserId };
                _db.Categories.Add(category);
                await _db.SaveChangesAsync();
            }
            return category;
        }

        private async Task<IEnumerable<SelectListItem>> GetCategoryOptionsAsync() =>
            (await _db.Categories.Where(c => c.UserId == CurrentUserId).OrderBy(c => c.Name).ToListAsync())
                .Select(c => new SelectListItem(CategoryDisplay.Localize(c.Name, _localizer), c.Id.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetAccountOptionsAsync() =>
            (await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync())
                .Select(a => new SelectListItem(AccountDisplay.Localize(a.Name, _localizer), a.Id.ToString()));

        internal static List<(string Value, string Label)> BuildMonthTabs(IStringLocalizer<SharedResource> localizer)
        {
            var tabs = new List<(string, string)> { ("all", localizer["Common_All"]) };
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            for (var i = 0; i < 6; i++)
            {
                var month = start.AddMonths(-i);
                tabs.Add((month.ToString("yyyy-MM"), month.ToString("MMM yyyy")));
            }
            return tabs;
        }
    }
}
