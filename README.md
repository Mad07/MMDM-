# MMDM Expense Tracker (Mark1)

Personal expense/income tracker built with ASP.NET Core MVC (.NET 8) + EF Core + PostgreSQL.

This repository is a from-scratch reimplementation of the app described in
[`PROJECT_STATUS.md`](PROJECT_STATUS.md), built directly in this repo rather than
copied from the original local machine (the source there was never available to
this environment - only the handoff summary was).

Source lives under [`src/Mark1`](src/Mark1).

## What's built

- Full CRUD for Expenses and Incomes, both currencies (USD/CRC) supported per transaction.
- Categories and Accounts as user-managed lookup tables (add/delete freely, scoped per user).
- Soft-delete on Expenses (delete -> hide from list, "Deleted" history page with Restore).
- Settings hub (tab-based): Manage Categories, Manage Accounts, Currency Rate (fixed rate +
  optional live-rate lookup via api.frankfurter.dev, silently falls back to the fixed rate
  on failure).
- Account Overview dashboard: grand totals (USD-only, CRC-only, and both mixed via the
  exchange rate), per-account collapsible cards, month/period picker (last 12 months + All Time).
- Filtering on Expenses/Incomes lists: date range, category, account, currency, plus a
  6-month tab strip on both lists.
- Recurring transactions: mark an Expense/Income "repeats monthly," auto-generates the next
  occurrence when due (checked lazily on page load, throttled to once/minute).
- Home dashboard: stat tiles, recent activity tables, 4 Chart.js charts (spending-by-category
  and 6-month trend, each split USD/CRC).
- Full bilingual UI (English/Spanish, es-CR culture) via `IStringLocalizer<SharedResource>` +
  `Resources/SharedResource.resx` + `SharedResource.es.resx`, with an EN/ES toggle in the nav
  (and on the login page).
- Custom visual theme (brand colors, semantic income=green/expense=red) in `wwwroot/css/site.css`.
- Full ASP.NET Core Identity: hand-written Login/Logout/AccessDenied pages, every
  Expense/Income/Account/Category/AppSettings row scoped to its owning user, self-registration
  present in code but disabled by default (`Features:SelfRegistrationEnabled` in
  `appsettings.json`), 60-day sliding login cookie, and a startup seeder that creates the first
  account from local user-secrets if no users exist yet.

## Not yet done / next steps

1. **Azure deployment** - Azure account exists now; App Service + Azure Database for PostgreSQL
   still need to be provisioned, secrets (seed password, connection string) moved to App Service
   configuration / Key Vault, and deploy via `az webapp up` or CI.
2. **Budgets per category** - not started (would be per-category, per-currency, monthly).

## How to run locally

Requires a reachable PostgreSQL server (a local install, a Docker container, or the Azure
Database for PostgreSQL instance used for this app).

```bash
cd src/Mark1
dotnet user-secrets init
dotnet user-secrets set "Seed:AdminEmail" "you@example.com"
dotnet user-secrets set "Seed:AdminPassword" "ChangeMe123"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=mark1;Username=postgres;Password=yourpassword"
export ASPNETCORE_ENVIRONMENT=Development   # required for user-secrets to load
dotnet build
dotnet run
# then browse to the URL printed in the console and log in with the seeded credentials
```

Migrations are applied automatically on first run (`Program.cs` calls `db.Database.Migrate()`
at startup) - the database itself just needs to already exist on the server.

### EF Core migrations

If you change the models, add a new migration (the `dotnet-ef` global tool must be installed:
`dotnet tool install --global dotnet-ef`):

```bash
cd src/Mark1
dotnet ef migrations add <MigrationName>
```
