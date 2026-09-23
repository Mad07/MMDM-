using System.ComponentModel.DataAnnotations;

namespace Mark1.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Whether this category can be picked when logging an Expense. Both this and
        /// IsForIncomes default true (usable for both) so existing categories keep working exactly
        /// as before - the user can narrow either one off per category in Settings.</summary>
        public bool IsForExpenses { get; set; } = true;

        /// <summary>Whether this category can be picked when logging an Income.</summary>
        public bool IsForIncomes { get; set; } = true;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Income> Incomes { get; set; } = new List<Income>();
    }
}
