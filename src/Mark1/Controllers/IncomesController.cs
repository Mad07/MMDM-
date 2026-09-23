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
    public class IncomesController : AuthorizedController
    {
        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public IncomesController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int? categoryId, int? accountId, Currency? currency, string? month, string? sortBy, string? sortDir)
        {
            var query = _db.Incomes
                .Include(i => i.Category)
                .Include(i => i.Account)
                .Where(i => i.UserId == CurrentUserId);

            if (!string.IsNullOrEmpty(month) && month != "all" && DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var monthStart))
            {
                var monthEnd = monthStart.AddMonths(1);
                query = query.Where(i => i.Date >= monthStart && i.Date < monthEnd);
            }
            else
            {
                if (dateFrom.HasValue) query = query.Where(i => i.Date >= dateFrom.Value);
                if (dateTo.HasValue) query = query.Where(i => i.Date <= dateTo.Value);
            }

            if (categoryId.HasValue) query = query.Where(i => i.CategoryId == categoryId.Value);
            if (accountId.HasValue) query = query.Where(i => i.AccountId == accountId.Value);
            if (currency.HasValue) query = query.Where(i => i.Currency == currency.Value);

            var effectiveSortBy = string.IsNullOrEmpty(sortBy) ? "date" : sortBy;
            var effectiveSortDescending = string.IsNullOrEmpty(sortBy) || sortDir == "desc";

            IOrderedQueryable<Income> ordered = effectiveSortBy switch
            {
                "description" => effectiveSortDescending ? query.OrderByDescending(i => i.Description) : query.OrderBy(i => i.Description),
                "category" => effectiveSortDescending ? query.OrderByDescending(i => i.Category!.Name) : query.OrderBy(i => i.Category!.Name),
                "account" => effectiveSortDescending ? query.OrderByDescending(i => i.Account!.Name) : query.OrderBy(i => i.Account!.Name),
                "amount" => effectiveSortDescending ? query.OrderByDescending(i => i.Amount) : query.OrderBy(i => i.Amount),
                _ => effectiveSortDescending ? query.OrderByDescending(i => i.Date) : query.OrderBy(i => i.Date),
            };

            var items = await ordered.ToListAsync();

            var vm = new TransactionFilterViewModel<Income>
            {
                Items = items,
                TotalUsd = items.Where(i => i.Currency == Currency.USD).Sum(i => i.Amount),
                TotalCrc = items.Where(i => i.Currency == Currency.CRC).Sum(i => i.Amount),
                DateFrom = dateFrom,
                DateTo = dateTo,
                CategoryId = categoryId,
                AccountId = accountId,
                Currency = currency,
                Month = string.IsNullOrEmpty(month) ? "all" : month,
                SortBy = effectiveSortBy,
                SortDescending = effectiveSortDescending,
                MonthTabs = ExpensesController.BuildMonthTabs(_localizer),
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Create(string? returnUrl)
        {
            var settings = await _db.AppSettings.FirstOrDefaultAsync(s => s.UserId == CurrentUserId);
            var vm = new IncomeFormViewModel
            {
                Currency = settings?.PrimaryCurrency ?? Currency.USD,
                ReturnUrl = returnUrl,
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomeFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryOptions = await GetCategoryOptionsAsync();
                model.AccountOptions = await GetAccountOptionsAsync();
                return View(model);
            }

            var income = new Income
            {
                Description = model.Description,
                Amount = model.Amount,
                Currency = model.Currency,
                Date = model.Date,
                CategoryId = model.CategoryId,
                AccountId = model.AccountId,
                RepeatsMonthly = model.RepeatsMonthly,
                NextOccurrenceDate = model.RepeatsMonthly ? model.Date.AddMonths(1) : null,
                UserId = CurrentUserId
            };
            _db.Incomes.Add(income);
            await _db.SaveChangesAsync();
            return RedirectToLocalOrIndex(model.ReturnUrl);
        }

        public async Task<IActionResult> Edit(int id, string? returnUrl)
        {
            var income = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (income == null) return NotFound();

            var vm = new IncomeFormViewModel
            {
                Id = income.Id,
                Description = income.Description,
                Amount = income.Amount,
                Currency = income.Currency,
                Date = income.Date,
                CategoryId = income.CategoryId,
                AccountId = income.AccountId,
                RepeatsMonthly = income.RepeatsMonthly,
                ReturnUrl = returnUrl,
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IncomeFormViewModel model)
        {
            var income = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (income == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.CategoryOptions = await GetCategoryOptionsAsync();
                model.AccountOptions = await GetAccountOptionsAsync();
                return View(model);
            }

            income.Description = model.Description;
            income.Amount = model.Amount;
            income.Currency = model.Currency;
            income.Date = model.Date;
            income.CategoryId = model.CategoryId;
            income.AccountId = model.AccountId;

            if (model.RepeatsMonthly && !income.RepeatsMonthly)
            {
                income.NextOccurrenceDate = model.Date.AddMonths(1);
            }
            else if (!model.RepeatsMonthly)
            {
                income.NextOccurrenceDate = null;
            }
            income.RepeatsMonthly = model.RepeatsMonthly;

            await _db.SaveChangesAsync();
            return RedirectToLocalOrIndex(model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var income = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == CurrentUserId);
            if (income != null)
            {
                _db.Incomes.Remove(income);
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

        private async Task<IEnumerable<SelectListItem>> GetCategoryOptionsAsync() =>
            (await _db.Categories.Where(c => c.UserId == CurrentUserId).OrderBy(c => c.Name).ToListAsync())
                .Select(c => new SelectListItem(CategoryDisplay.Localize(c.Name, _localizer), c.Id.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetAccountOptionsAsync() =>
            (await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync())
                .Select(a => new SelectListItem(AccountDisplay.Localize(a.Name, _localizer), a.Id.ToString()));
    }
}
