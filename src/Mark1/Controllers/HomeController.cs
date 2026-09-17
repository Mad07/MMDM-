using System.Diagnostics;
using Mark1.Data;
using Mark1.Models;
using Mark1.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mark1.Controllers;

public class HomeController : AuthorizedController
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var expensesThisMonth = await _db.Expenses
            .Where(e => e.UserId == CurrentUserId && !e.IsDeleted && e.Date >= monthStart && e.Date < monthEnd)
            .ToListAsync();
        var incomesThisMonth = await _db.Incomes
            .Where(i => i.UserId == CurrentUserId && i.Date >= monthStart && i.Date < monthEnd)
            .ToListAsync();

        var vm = new HomeDashboardViewModel
        {
            SpendingThisMonthUsd = expensesThisMonth.Where(e => e.Currency == Currency.USD).Sum(e => e.Amount),
            SpendingThisMonthCrc = expensesThisMonth.Where(e => e.Currency == Currency.CRC).Sum(e => e.Amount),
            IncomeThisMonthUsd = incomesThisMonth.Where(i => i.Currency == Currency.USD).Sum(i => i.Amount),
            IncomeThisMonthCrc = incomesThisMonth.Where(i => i.Currency == Currency.CRC).Sum(i => i.Amount),
            ExpenseCount = expensesThisMonth.Count,
            IncomeCount = incomesThisMonth.Count
        };

        vm.RecentExpenses = await _db.Expenses
            .Include(e => e.Category).Include(e => e.Account)
            .Where(e => e.UserId == CurrentUserId && !e.IsDeleted)
            .OrderByDescending(e => e.Date).Take(5).ToListAsync();

        vm.RecentIncomes = await _db.Incomes
            .Include(i => i.Category).Include(i => i.Account)
            .Where(i => i.UserId == CurrentUserId)
            .OrderByDescending(i => i.Date).Take(5).ToListAsync();

        var byCategoryUsd = expensesThisMonth.Where(e => e.Currency == Currency.USD)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { g.Key, Total = g.Sum(e => e.Amount) })
            .ToList();
        var byCategoryCrc = expensesThisMonth.Where(e => e.Currency == Currency.CRC)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { g.Key, Total = g.Sum(e => e.Amount) })
            .ToList();

        var categoryNames = await _db.Categories.Where(c => c.UserId == CurrentUserId).ToDictionaryAsync(c => c.Id, c => c.Name);

        vm.CategoryLabelsUsd = byCategoryUsd.Select(g => categoryNames.GetValueOrDefault(g.Key, "Other")).ToList();
        vm.CategoryValuesUsd = byCategoryUsd.Select(g => g.Total).ToList();
        vm.CategoryLabelsCrc = byCategoryCrc.Select(g => categoryNames.GetValueOrDefault(g.Key, "Other")).ToList();
        vm.CategoryValuesCrc = byCategoryCrc.Select(g => g.Total).ToList();

        var sixMonthsAgo = monthStart.AddMonths(-5);
        var trendExpenses = await _db.Expenses
            .Where(e => e.UserId == CurrentUserId && !e.IsDeleted && e.Date >= sixMonthsAgo)
            .ToListAsync();
        var trendIncomes = await _db.Incomes
            .Where(i => i.UserId == CurrentUserId && i.Date >= sixMonthsAgo)
            .ToListAsync();

        for (var i = 0; i < 6; i++)
        {
            var monthCursor = sixMonthsAgo.AddMonths(i);
            var cursorEnd = monthCursor.AddMonths(1);
            vm.TrendLabels.Add(monthCursor.ToString("MMM yyyy"));

            vm.TrendExpensesUsd.Add(trendExpenses.Where(e => e.Currency == Currency.USD && e.Date >= monthCursor && e.Date < cursorEnd).Sum(e => e.Amount));
            vm.TrendExpensesCrc.Add(trendExpenses.Where(e => e.Currency == Currency.CRC && e.Date >= monthCursor && e.Date < cursorEnd).Sum(e => e.Amount));
            vm.TrendIncomeUsd.Add(trendIncomes.Where(i => i.Currency == Currency.USD && i.Date >= monthCursor && i.Date < cursorEnd).Sum(i => i.Amount));
            vm.TrendIncomeCrc.Add(trendIncomes.Where(i => i.Currency == Currency.CRC && i.Date >= monthCursor && i.Date < cursorEnd).Sum(i => i.Amount));
        }

        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
