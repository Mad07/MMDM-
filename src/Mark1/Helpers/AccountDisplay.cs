using Microsoft.Extensions.Localization;

namespace Mark1.Helpers
{
    /// <summary>
    /// Accounts are free-text rows a user can add or delete (Models/Account.cs), so most can't be
    /// translated - only the fixed set of names IdentitySeeder seeds by default has resource
    /// entries. Anything else (a user's own account) is shown exactly as typed.
    /// </summary>
    public static class AccountDisplay
    {
        private static readonly HashSet<string> DefaultAccountNames = new(StringComparer.Ordinal)
        {
            "Cash", "Savings", "Checking"
        };

        public static string Localize(string? name, IStringLocalizer localizer)
        {
            if (string.IsNullOrEmpty(name) || !DefaultAccountNames.Contains(name))
            {
                return name ?? string.Empty;
            }

            var localized = localizer[$"Account_{name}"];
            return localized.ResourceNotFound ? name : localized.Value;
        }
    }
}
