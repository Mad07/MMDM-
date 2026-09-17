using Mark1.Data;
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

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int? categoryId, int? accountId, Currency? currency, string? month)
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

            var items = await query.OrderByDescending(i => i.Date).ToListAsync();

            var vm = new TransactionFilterViewModel<Income>
            {
                Items = items,
                DateFrom = dateFrom,
                DateTo = dateTo,
                CategoryId = categoryId,
                AccountId = accountId,
                Currency = currency,
                Month = string.IsNullOrEmpty(month) ? "all" : month,
                MonthTabs = ExpensesController.BuildMonthTabs(_localizer),
                CategoryOptions = await GetCategoryOptionsAsync(),
                AccountOptions = await GetAccountOptionsAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new IncomeFormViewModel
            {
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
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
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
            return RedirectToAction(nameof(Index));
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
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetCategoryOptionsAsync() =>
            (await _db.Categories.Where(c => c.UserId == CurrentUserId).OrderBy(c => c.Name).ToListAsync())
                .Select(c => new SelectListItem(c.Name, c.Id.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetAccountOptionsAsync() =>
            (await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync())
                .Select(a => new SelectListItem(a.Name, a.Id.ToString()));
    }
}
