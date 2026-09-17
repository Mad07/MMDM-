namespace Mark1.Services
{
    public interface IRecurringTransactionService
    {
        /// <summary>Generates the next occurrence for any due recurring Expense/Income belonging to the user.</summary>
        Task ProcessDueRecurringAsync(string userId);
    }
}
