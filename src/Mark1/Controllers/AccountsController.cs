using Mark1.Data;
using Mark1.Helpers;
using Mark1.Models;
using Mark1.Models.ViewModels;
using Mark1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Mark1.Controllers
{
    /// <summary>The read-only per-account balance dashboard. Managing accounts (add/delete) lives under Settings.</summary>
    public class AccountsController : AuthorizedController
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountsController(ApplicationDbContext db, ICurrencyService currencyService, IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            _currencyService = currencyService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(string period = "all")
        {
            var settings = await _db.AppSettings.FirstOrDefaultAsync(s => s.UserId == CurrentUserId)
                ?? new AppSettings { FixedExchangeRate = 525m, UseLiveRate = false };
            var rate = await _currencyService.GetUsdToCrcRateAsync(settings.FixedExchangeRate, settings.UseLiveRate);

            var expensesQuery = _db.Expenses.Include(e => e.Account).Where(e => e.UserId == CurrentUserId && !e.IsDeleted);
            var incomesQuery = _db.Incomes.Include(i => i.Account).Where(i => i.UserId == CurrentUserId);

            if (period != "all" && DateTime.TryParseExact(period + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var periodStart))
            {
                var periodEnd = periodStart.AddMonths(1);
                expensesQuery = expensesQuery.Where(e => e.Date >= periodStart && e.Date < periodEnd);
                incomesQuery = incomesQuery.Where(i => i.Date >= periodStart && i.Date < periodEnd);
            }

            var expenses = await expensesQuery.ToListAsync();
            var incomes = await incomesQuery.ToListAsync();

            var accounts = await _db.Accounts.Where(a => a.UserId == CurrentUserId).OrderBy(a => a.Name).ToListAsync();
            var purposeNames = await _db.SavingsPurposes.Where(sp => sp.UserId == CurrentUserId).ToDictionaryAsync(sp => sp.Id, sp => sp.Name);

            var vm = new AccountOverviewViewModel
            {
                PrimaryCurrency = settings.PrimaryCurrency,
                SelectedPeriod = period,
                PeriodOptions = BuildPeriodOptions(_localizer)
            };

            foreach (var account in accounts)
            {
                var accountIncomeUsd = incomes.Where(i => i.AccountId == account.Id && i.Currency == Currency.USD).Sum(i => i.Amount);
                var accountIncomeCrc = incomes.Where(i => i.AccountId == account.Id && i.Currency == Currency.CRC).Sum(i => i.Amount);
                var accountExpenseUsd = expenses.Where(e => e.AccountId == account.Id && e.Currency == Currency.USD && e.IsPaid).Sum(e => e.Amount);
                var accountExpenseCrc = expenses.Where(e => e.AccountId == account.Id && e.Currency == Currency.CRC && e.IsPaid).Sum(e => e.Amount);

                var netUsd = accountIncomeUsd - accountExpenseUsd;
                var netCrc = accountIncomeCrc - accountExpenseCrc;
                var netMixedUsd = netUsd + (rate > 0 ? netCrc / rate : 0);

                var hasUsdActivity = incomes.Any(i => i.AccountId == account.Id && i.Currency == Currency.USD)
                    || expenses.Any(e => e.AccountId == account.Id && e.Currency == Currency.USD);
                var hasCrcActivity = incomes.Any(i => i.AccountId == account.Id && i.Currency == Currency.CRC)
                    || expenses.Any(e => e.AccountId == account.Id && e.Currency == Currency.CRC);

                var card = new AccountCardViewModel
                {
                    AccountName = AccountDisplay.Localize(account.Name, _localizer),
                    NetUsd = netUsd,
                    NetCrc = netCrc,
                    NetMixedUsd = netMixedUsd,
                    HasUsdActivity = hasUsdActivity,
                    HasCrcActivity = hasCrcActivity
                };

                if (account.Name == "Savings" || account.Name == "Retained")
                {
                    var transferredExpenses = account.Name == "Savings"
                        ? expenses.Where(e => e.IsTransferToSavings && e.IsPaid)
                        : expenses.Where(e => e.IsTransferToRetained && e.IsPaid);

                    // Incomes auto-created by a transfer expense are already represented via that
                    // expense above - only fold in incomes the user entered directly on this account
                    // (e.g. a manual deposit), so nothing gets double-counted.
                    var transferIncomeIds = expenses.Where(e => e.TransferIncomeId.HasValue).Select(e => e.TransferIncomeId!.Value).ToHashSet();
                    var manualIncomes = incomes.Where(i => i.AccountId == account.Id && !transferIncomeIds.Contains(i.Id));

                    card.PurposeBreakdown = transferredExpenses
                        .Select(e => new { e.SavingsPurposeId, AmountUsd = e.Currency == Currency.USD ? e.Amount : 0m, AmountCrc = e.Currency == Currency.CRC ? e.Amount : 0m })
                        .Concat(manualIncomes.Select(i => new { i.SavingsPurposeId, AmountUsd = i.Currency == Currency.USD ? i.Amount : 0m, AmountCrc = i.Currency == Currency.CRC ? i.Amount : 0m }))
                        .GroupBy(x => x.SavingsPurposeId)
                        .Select(g => new SavingsPurposeBreakdownItem
                        {
                            PurposeName = g.Key.HasValue && purposeNames.TryGetValue(g.Key.Value, out var name)
                                ? name
                                : _localizer["Common_NoPurpose"],
                            AmountUsd = g.Sum(x => x.AmountUsd),
                            AmountCrc = g.Sum(x => x.AmountCrc)
                        })
                        .OrderBy(b => b.PurposeName)
                        .ToList();
                }

                vm.AccountCards.Add(card);
            }

            vm.GrandTotalUsdOnly = incomes.Where(i => i.Currency == Currency.USD).Sum(i => i.Amount)
                - expenses.Where(e => e.Currency == Currency.USD && e.IsPaid).Sum(e => e.Amount);
            vm.GrandTotalCrcOnly = incomes.Where(i => i.Currency == Currency.CRC).Sum(i => i.Amount)
                - expenses.Where(e => e.Currency == Currency.CRC && e.IsPaid).Sum(e => e.Amount);
            vm.GrandTotalUsdMixed = vm.GrandTotalUsdOnly + (rate > 0 ? vm.GrandTotalCrcOnly / rate : 0);
            vm.GrandTotalCrcMixed = vm.GrandTotalCrcOnly + (vm.GrandTotalUsdOnly * rate);

            return View(vm);
        }

        private static List<(string, string)> BuildPeriodOptions(IStringLocalizer<SharedResource> localizer)
        {
            var options = new List<(string, string)> { ("all", localizer["Common_AllTime"]) };
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            for (var i = 0; i < 12; i++)
            {
                var month = start.AddMonths(-i);
                options.Add((month.ToString("yyyy-MM"), month.ToString("MMMM yyyy")));
            }
            return options;
        }
    }
}
