using Microsoft.Extensions.Localization;

namespace Mark1.Helpers
{
    /// <summary>
    /// Categories are free-text rows a user can add or delete (Models/Category.cs), so most can't
    /// be translated - only the fixed set of names IdentitySeeder seeds by default has resource
    /// entries. Anything else (a user's own category) is shown exactly as typed.
    /// </summary>
    public static class CategoryDisplay
    {
        private static readonly HashSet<string> DefaultCategoryNames = new(StringComparer.Ordinal)
        {
            "Groceries", "Rent", "Utilities", "Transportation", "Entertainment", "Salary", "Other"
        };

        public static string Localize(string? name, IStringLocalizer localizer)
        {
            if (string.IsNullOrEmpty(name) || !DefaultCategoryNames.Contains(name))
            {
                return name ?? string.Empty;
            }

            var localized = localizer[$"Category_{name}"];
            return localized.ResourceNotFound ? name : localized.Value;
        }
    }
}
