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

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int? categoryId, int? accountId, Currency? currency, string? month, string? sortBy, string? sortDir)
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

            // No sortBy in the URL yet -> unchanged default (most recent first). Once a column has
            // been clicked, direction comes straight from sortDir (the view computes the toggle).
            var effectiveSortBy = string.IsNullOrEmpty(sortBy) ? "date" : sortBy;
            var effectiveSortDescending = string.IsNullOrEmpty(sortBy) || sortDir == "desc";

            IOrderedQueryable<Expense> ordered = effectiveSortBy switch
            {
                "description" => effectiveSortDescending ? query.OrderByDescending(e => e.Description) : query.OrderBy(e => e.Description),
                "category" => effectiveSortDescending ? query.OrderByDescending(e => e.Category!.Name) : query.OrderBy(e => e.Category!.Name),
                "account" => effectiveSortDescending ? query.OrderByDescending(e => e.Account!.Name) : query.OrderBy(e => e.Account!.Name),
                "amount" => effectiveSortDescending ? query.OrderByDescending(e => e.Amount) : query.OrderBy(e => e.Amount),
                "paid" => effectiveSortDescending ? query.OrderByDescending(e => e.IsPaid) : query.OrderBy(e => e.IsPaid),
                _ => effectiveSortDescending ? query.OrderByDescending(e => e.Date) : query.OrderBy(e => e.Date),
            };

            var items = await ordered.ToListAsync();

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
                SortBy = effectiveSortBy,
                SortDescending = effectiveSortDescending,
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

        public async Task<IActionResult> Create(string? returnUrl)
        {
            var settings = await _db.AppSettings.FirstOrDefaultAsync(s => s.UserId == CurrentUserId);
            var vm = new ExpenseFormViewModel
            {
                Currency = settings?.PrimaryCurrency ?? Currency.USD,
                ReturnUrl = returnUrl,
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync(),
                SavingsPurposeOptions = await GetSavingsPurposeOptionsAsync()
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
                model.SavingsPurposeOptions = await GetSavingsPurposeOptionsAsync();
                return View(model);
            }

            var isTransferring = model.IsTransferToSavings || model.IsTransferToRetained;
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
                IsTransferToRetained = model.IsTransferToRetained,
                SavingsPurposeId = isTransferring ? model.SavingsPurposeId : null,
                UserId = CurrentUserId
            };
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            await SyncTransferIncomeAsync(expense, GetTransferTargetAccountName(model.IsTransferToSavings, model.IsTransferToRetained));
            await _db.SaveChangesAsync();

            return RedirectToLocalOrIndex(model.ReturnUrl);
        }

        public async Task<IActionResult> Edit(int id, string? returnUrl)
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
                IsTransferToRetained = expense.IsTransferToRetained,
                SavingsPurposeId = expense.SavingsPurposeId,
                ReturnUrl = returnUrl,
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync(),
                SavingsPurposeOptions = await GetSavingsPurposeOptionsAsync()
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
                model.SavingsPurposeOptions = await GetSavingsPurposeOptionsAsync();
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
            expense.IsTransferToRetained = model.IsTransferToRetained;
            expense.SavingsPurposeId = (model.IsTransferToSavings || model.IsTransferToRetained) ? model.SavingsPurposeId : null;

            await SyncTransferIncomeAsync(expense, GetTransferTargetAccountName(model.IsTransferToSavings, model.IsTransferToRetained));
            await _db.SaveChangesAsync();
            return RedirectToLocalOrIndex(model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePaid(int id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == CurrentUserId);
            if (expense == null) return NotFound();

            expense.IsPaid = !expense.IsPaid;
            await _db.SaveChangesAsync();
            return NoContent();
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
                // is later restored, the transfer link is gone and both checkboxes revert to unchecked.
                await SyncTransferIncomeAsync(expense, targetAccountName: null);
                expense.IsTransferToSavings = false;
                expense.IsTransferToRetained = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToReferrerOrIndex();
        }

        /// <summary>Redirects to returnUrl if it's set and points at this app, otherwise Index -
        /// used by Create/Edit so saving from a filtered/sorted list view returns to that same view
        /// instead of always resetting to the plain unfiltered list.</summary>
        private IActionResult RedirectToLocalOrIndex(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Same idea as RedirectToLocalOrIndex, but for actions (like Delete) whose button
        /// lives directly on the Index page itself, so the Referer header already is that filtered
        /// URL - no returnUrl field needed.</summary>
        private IActionResult RedirectToReferrerOrIndex()
        {
            var referer = Request.Headers.Referer.ToString();
            if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) && refererUri.Host == Request.Host.Host)
            {
                return Redirect(refererUri.PathAndQuery);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Savings takes priority if both checkboxes somehow arrive true (client-side JS
        /// keeps them mutually exclusive, but this is the server-side fallback).</summary>
        private static string? GetTransferTargetAccountName(bool toSavings, bool toRetained)
        {
            if (toSavings) return "Savings";
            if (toRetained) return "Retained";
            return null;
        }

        /// <summary>Keeps an expense's mirrored destination-account Income in sync: creates one if
        /// targetAccountName is set, updates it in place if the expense's amount/date/target/etc.
        /// changed, or removes it if targetAccountName is null. No-ops (with a warning) if the user
        /// has no account with that literal name - leaves the expense as a plain expense rather
        /// than failing the save.</summary>
        private async Task SyncTransferIncomeAsync(Expense expense, string? targetAccountName)
        {
            if (targetAccountName == null)
            {
                if (expense.TransferIncomeId.HasValue)
                {
                    var existing = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == expense.TransferIncomeId.Value && i.UserId == CurrentUserId);
                    if (existing != null) _db.Incomes.Remove(existing);
                    expense.TransferIncomeId = null;
                }
                return;
            }

            var targetAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == CurrentUserId && a.Name == targetAccountName);
            if (targetAccount == null)
            {
                TempData["ExpenseTransferWarning"] = _localizer["Expenses_TransferNoAccount", targetAccountName].Value;
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

            income.Description = _localizer["Expenses_TransferIncomeDescription", AccountDisplay.Localize(targetAccountName, _localizer), expense.Description];
            income.Amount = expense.Amount;
            income.Currency = expense.Currency;
            income.Date = expense.Date;
            income.CategoryId = transferCategory.Id;
            income.AccountId = targetAccount.Id;
            income.SavingsPurposeId = expense.SavingsPurposeId;

            await _db.SaveChangesAsync();
            expense.TransferIncomeId = income.Id;
        }

        private async Task<Category> GetOrCreateTransferCategoryAsync()
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.Name == "Transfer");
            if (category == null)
            {
                // Only ever assigned to the auto-generated Income side of a transfer - the source
                // expense keeps whatever category the user actually picked on it.
                category = new Category { Name = "Transfer", IsForExpenses = false, IsForIncomes = true, UserId = CurrentUserId };
                _db.Categories.Add(category);
                await _db.SaveChangesAsync();
            }
            return category;
        }

        private async Task<IEnumerable<SelectListItem>> GetCategoryOptionsAsync() =>
            (await _db.Categories.Where(c => c.UserId == CurrentUserId && c.IsForExpenses).OrderBy(c => c.Name).ToListAsync())
                .Select(c => new SelectListItem(CategoryDisplay.Localize(c.Name, _localizer), c.Id.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetAccountOptionsAsync() =>
            (await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync())
                .Select(a => new SelectListItem(AccountDisplay.Localize(a.Name, _localizer), a.Id.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetSavingsPurposeOptionsAsync() =>
            (await _db.SavingsPurposes.Where(sp => sp.UserId == CurrentUserId).OrderBy(sp => sp.Name).ToListAsync())
                .Select(sp => new SelectListItem(sp.Name, sp.Id.ToString()));

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
