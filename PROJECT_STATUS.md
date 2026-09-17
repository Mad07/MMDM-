# MMDM Expense Tracker — Project Status

Personal expense/income tracker built with ASP.NET Core MVC (.NET 8) + EF Core + SQLite.
Location: `C:\Users\MDelgad104\OneDrive - T-Mobile USA\Documents\Dev Training\Mark1`

## What's built

**Core features**
- Full CRUD for Expenses and Incomes, both currencies (USD/CRC) supported per transaction.
- Categories and Accounts as user-managed lookup tables (not hardcoded — add/delete freely).
- Soft-delete on Expenses (delete → hide from list, "Deleted" history page with Restore).
- Settings hub (tab-based): Manage Categories, Manage Accounts, Currency Rate (fixed rate + optional live-rate lookup via api.frankfurter.dev, silently falls back to fixed rate on failure — this network's proxy blocks the live API, so it always falls back here).
- Account Overview dashboard: grand totals (USD-only, CRC-only, USD-mixed, CRC-mixed via exchange rate), per-account collapsible cards, month/year period picker (last 12 months + All Time).
- Filtering on Expenses/Incomes lists: date range, category, account, currency — plus a 6-month tab strip on Expenses.
- Recurring transactions: mark an Expense/Income "repeats monthly," auto-generates the next occurrence when due (checked lazily on page load, throttled to once/minute, not a background timer).
- Home dashboard: stat tiles (spending/income totals, counts), recent activity tables, 4 charts (spending-by-category and 6-month trend, each split USD/CRC via Chart.js).
- Full bilingual UI (English/Spanish, es-CR culture) via `IStringLocalizer` + `Resources/SharedResource.resx` + `SharedResource.es.resx`. Small EN/ES toggle badge, top-right corner.
- Custom visual theme (brand colors, semantic income=green/expense=red, polished cards/tables) in `wwwroot/css/site.css`.

**Authentication (just completed)**
- Full ASP.NET Core Identity (`ApplicationUser : IdentityUser`, merged into `ApplicationDbContext : IdentityDbContext<ApplicationUser>`).
- Every Expense/Income/Account/Category/AppSettings row now belongs to a specific user (`UserId` FK, required, cascade-delete). Every controller scopes every query and every direct-Id lookup (Edit/Delete/etc.) by the logged-in user — no cross-user data leakage.
- Hand-written Login/Logout/AccessDenied pages (`AccountController`, `Views/Account/*`) — no Identity UI scaffolding, matches the app's existing hand-written view style.
- Self-registration exists in code (`Register` action/view) but is **disabled** via `appsettings.json` → `Features:SelfRegistrationEnabled: false`. Flip to `true` to enable it later — no code changes needed.
- Login cookie: 60-day expiration, sliding.
- First (only) real account seeded automatically at startup if no users exist yet (`Data/IdentitySeeder.cs`), using credentials from local `dotnet user-secrets` (`Seed:AdminEmail`, `Seed:AdminPassword`) — **not** committed to source. Current login: `07mdelgado@gmail.com`.
- All pre-existing real financial data (accounts: Cash, Savings, Salario BAC, Colones BAC/BCR/BN, Dolares BCR; expenses/incomes/categories) was carefully backfilled to this account during migration — verified no data loss.

## Migrations applied (in order)
1. `InitialCreate` — original schema.
2. `AddCurrencyAndSettings`, `AddAccountsIncomesAndSoftDelete`, `AddRecurrenceFields` — feature additions.
3. `AddIdentityAndUserId` — Identity tables + nullable `UserId` columns (hand-edited to remove auto-generated `DeleteData` calls that would have destroyed the original seeded Category/Account/AppSettings rows still referenced by real data).
4. `MakeUserIdRequired` — flipped `UserId` to NOT NULL + added FK constraints, after confirming all rows were backfilled.

## Known local dev quirks (not bugs, just environment notes)
- Running via `dotnet run`/the built `.exe` sometimes fails with "Access is denied" on this OneDrive-synced folder — running the DLL directly (`dotnet bin/Debug/net8.0/Mark1.dll`) works around it.
- User-secrets only load when `ASPNETCORE_ENVIRONMENT=Development` is set — must export that env var before running locally, otherwise the seeder silently finds no config and creates no user.
- `dotnet-ef` must be run via the global tool directly (add `~/.dotnet/tools` to PATH) — the local `dotnet ef` invocation doesn't work in this project (no tool manifest was created).
- Corporate network proxy blocks FX API domains (frankfurter.dev, open.er-api.com) and Bootstrap Icons/Chart.js CDNs are reachable (only FX-data-style domains are blocked).

## Not yet done / next steps
1. **Azure deployment** — user doesn't have an Azure account set up yet. Plan (agreed but not started):
   - Create Azure account/subscription.
   - Migrate off SQLite to Azure SQL Database or Postgres (swap EF Core provider, regenerate migrations, move real data over).
   - Move secrets (seed password, connection string) to Azure App Service configuration / Key Vault — not `appsettings.json`.
   - Deploy via `az webapp up` or a CI pipeline.
2. **Budgets per category** — explicitly put on hold by the user earlier (was going to be per-category, per-currency, monthly). Not started.
3. Nothing else currently pending — auth was the last completed milestone before this handoff.

## How to run locally
```bash
cd "Dev Training/Mark1"
export ASPNETCORE_ENVIRONMENT=Development   # required for user-secrets to load
dotnet build
dotnet bin/Debug/net8.0/Mark1.dll
# then browse to http://localhost:5000, log in with the seeded credentials
```
