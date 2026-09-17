using Mark1.Data;
using Mark1.Models;
using Microsoft.EntityFrameworkCore;

namespace Mark1.Services
{
    public class RecurringTransactionService : IRecurringTransactionService
    {
        private readonly ApplicationDbContext _db;

        public RecurringTransactionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task ProcessDueRecurringAsync(string userId)
        {
            var today = DateTime.Today;

            var dueExpenses = await _db.Expenses
                .Where(e => e.UserId == userId && e.RepeatsMonthly && !e.IsDeleted
                    && e.NextOccurrenceDate != null && e.NextOccurrenceDate <= today)
                .ToListAsync();

            foreach (var template in dueExpenses)
            {
                var nextDate = template.NextOccurrenceDate!.Value;
                _db.Expenses.Add(new Expense
                {
                    Description = template.Description,
                    Amount = template.Amount,
                    Currency = template.Currency,
                    Date = nextDate,
                    CategoryId = template.CategoryId,
                    AccountId = template.AccountId,
                    UserId = template.UserId,
                    RepeatsMonthly = true,
                    NextOccurrenceDate = nextDate.AddMonths(1)
                });
                template.NextOccurrenceDate = null;
            }

            var dueIncomes = await _db.Incomes
                .Where(i => i.UserId == userId && i.RepeatsMonthly
                    && i.NextOccurrenceDate != null && i.NextOccurrenceDate <= today)
                .ToListAsync();

            foreach (var template in dueIncomes)
            {
                var nextDate = template.NextOccurrenceDate!.Value;
                _db.Incomes.Add(new Income
                {
                    Description = template.Description,
                    Amount = template.Amount,
                    Currency = template.Currency,
                    Date = nextDate,
                    CategoryId = template.CategoryId,
                    AccountId = template.AccountId,
                    UserId = template.UserId,
                    RepeatsMonthly = true,
                    NextOccurrenceDate = nextDate.AddMonths(1)
                });
                template.NextOccurrenceDate = null;
            }

            if (dueExpenses.Count > 0 || dueIncomes.Count > 0)
            {
                await _db.SaveChangesAsync();
            }
        }
    }
}
