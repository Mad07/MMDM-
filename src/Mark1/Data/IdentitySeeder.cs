using Mark1.Models;
using Microsoft.AspNetCore.Identity;

namespace Mark1.Data
{
    /// <summary>
    /// Seeds the first (only) real account at startup if no users exist yet, using credentials from
    /// user-secrets (Seed:AdminEmail / Seed:AdminPassword). Never runs if a user already exists, and
    /// never reads its credentials from appsettings.json - those secrets are local-only.
    /// </summary>
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            if (userManager.Users.Any())
            {
                return;
            }

            var email = configuration["Seed:AdminEmail"];
            var password = configuration["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return;
            }

            var defaultCategories = new[] { "Groceries", "Rent", "Utilities", "Transportation", "Entertainment", "Salary", "Other" };
            foreach (var name in defaultCategories)
            {
                db.Categories.Add(new Category { Name = name, UserId = user.Id });
            }

            var defaultAccounts = new[] { "Cash", "Savings", "Checking" };
            foreach (var name in defaultAccounts)
            {
                db.Accounts.Add(new Account { Name = name, UserId = user.Id });
            }

            db.AppSettings.Add(new AppSettings
            {
                UserId = user.Id,
                FixedExchangeRate = 525m,
                UseLiveRate = true
            });

            await db.SaveChangesAsync();
        }
    }
}
