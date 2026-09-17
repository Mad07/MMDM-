using Microsoft.AspNetCore.Identity;

namespace Mark1.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Income> Incomes { get; set; } = new List<Income>();
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
