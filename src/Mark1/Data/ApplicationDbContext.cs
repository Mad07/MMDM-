using Mark1.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mark1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Income> Incomes => Set<Income>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<AppSettings> AppSettings => Set<AppSettings>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Expense>()
                .HasOne(e => e.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Expense>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Expense>()
                .HasOne(e => e.Account)
                .WithMany(a => a.Expenses)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Date-only fields, not points in time - "date" avoids Npgsql's UTC-only Kind
            // requirement for "timestamp with time zone" (the DateTime default mapping).
            builder.Entity<Expense>().Property(e => e.Date).HasColumnType("date");
            builder.Entity<Expense>().Property(e => e.NextOccurrenceDate).HasColumnType("date");

            // Existing rows predate the Paid flag and already happened, so they backfill as paid;
            // ExpenseFormViewModel defaults new Create-form expenses to unpaid instead.
            builder.Entity<Expense>().Property(e => e.IsPaid).HasDefaultValue(true);

            builder.Entity<Income>()
                .HasOne(i => i.User)
                .WithMany(u => u.Incomes)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Income>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Incomes)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Income>()
                .HasOne(i => i.Account)
                .WithMany(a => a.Incomes)
                .HasForeignKey(i => i.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Income>().Property(i => i.Date).HasColumnType("date");
            builder.Entity<Income>().Property(i => i.NextOccurrenceDate).HasColumnType("date");

            builder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Account>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppSettings>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<AppSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppSettings>()
                .HasIndex(s => s.UserId)
                .IsUnique();
        }
    }
}
