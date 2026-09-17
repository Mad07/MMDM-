using Mark1.Models;

namespace Mark1.Models.ViewModels
{
    public class HomeDashboardViewModel
    {
        public decimal SpendingThisMonthUsd { get; set; }
        public decimal SpendingThisMonthCrc { get; set; }
        public decimal IncomeThisMonthUsd { get; set; }
        public decimal IncomeThisMonthCrc { get; set; }
        public int ExpenseCount { get; set; }
        public int IncomeCount { get; set; }

        public List<Expense> RecentExpenses { get; set; } = new();
        public List<Income> RecentIncomes { get; set; } = new();

        public List<string> CategoryLabelsUsd { get; set; } = new();
        public List<decimal> CategoryValuesUsd { get; set; } = new();
        public List<string> CategoryLabelsCrc { get; set; } = new();
        public List<decimal> CategoryValuesCrc { get; set; } = new();

        public List<string> TrendLabels { get; set; } = new();
        public List<decimal> TrendExpensesUsd { get; set; } = new();
        public List<decimal> TrendIncomeUsd { get; set; } = new();
        public List<decimal> TrendExpensesCrc { get; set; } = new();
        public List<decimal> TrendIncomeCrc { get; set; } = new();
    }
}
