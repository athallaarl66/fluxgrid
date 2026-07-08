using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class FinanceDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        var currentYear = DateTime.UtcNow.Year;
        var currentMonth = DateTime.UtcNow.Month;

        if (await db.JournalEntries.AnyAsync(e => e.TenantId == tenantId && e.TransactionDate.Year == currentYear))
            return;

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM journal_entry_lines WHERE \"EntryId\" IN (SELECT \"Id\" FROM journal_entries WHERE \"TenantId\" = {0})", tenantId);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM journal_entries WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM budgets WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM accounting_periods WHERE \"TenantId\" = {0}", tenantId);

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var accounts = await db.ChartOfAccounts
            .Where(a => a.TenantId == tenantId)
            .ToDictionaryAsync(a => a.Code);

        var adminId = (await db.Users.FirstOrDefaultAsync(u => u.Username == "admin"))?.Id ?? Guid.Empty;

        var monthlyData = new (int year, int month, decimal revenue, decimal expense)[]
        {
            (2025, 1, 450_000_000m, 420_000_000m),
            (2025, 2, 480_000_000m, 435_000_000m),
            (2025, 3, 510_000_000m, 455_000_000m),
            (2025, 4, 530_000_000m, 470_000_000m),
            (2025, 5, 560_000_000m, 490_000_000m),
            (2025, 6, 590_000_000m, 510_000_000m),
            (2025, 7, 620_000_000m, 530_000_000m),
            (2025, 8, 640_000_000m, 545_000_000m),
            (2025, 9, 670_000_000m, 565_000_000m),
            (2025, 10, 700_000_000m, 580_000_000m),
            (2025, 11, 730_000_000m, 600_000_000m),
            (2025, 12, 760_000_000m, 620_000_000m),
            (2026, 1, 500_000_000m, 480_000_000m),
            (2026, 2, 530_000_000m, 495_000_000m),
            (2026, 3, 560_000_000m, 515_000_000m),
            (2026, 4, 590_000_000m, 530_000_000m),
            (2026, 5, 620_000_000m, 550_000_000m),
            (2026, 6, 650_000_000m, 570_000_000m),
            (2026, 7, 380_000_000m, 290_000_000m),
        };

        var periods = new List<AccountingPeriod>();
        var seenPeriods = new HashSet<(int year, int month)>();

        var entries = new List<JournalEntry>();
        var lines = new List<JournalEntryLine>();
        var entryNo = 1;

        foreach (var (year, month, revenue, expense) in monthlyData)
        {
            if (seenPeriods.Add((year, month)))
            {
                var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                var isPast = year < currentYear || (year == currentYear && month < currentMonth);

                periods.Add(new AccountingPeriod
                {
                    Id = Guid.NewGuid(),
                    Name = startDate.ToString("MMMM yyyy"),
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = isPast ? "CLOSED" : "OPEN",
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var period = periods[seenPeriods.Count - 1];
            var txDate = new DateTime(year, month, 15, 0, 0, 0, DateTimeKind.Utc);

            var revenueEntry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                EntryNo = $"JE-{entryNo:D4}",
                TransactionDate = txDate,
                Description = $"{period.Name} Revenue",
                Status = "POSTED",
                TotalAmount = revenue,
                CreatedBy = adminId,
                TenantId = tenantId,
                CreatedAt = txDate
            };

            lines.Add(new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                EntryId = revenueEntry.Id,
                AccountId = accounts["1110"].Id,
                Debit = revenue,
                Credit = 0
            });
            lines.Add(new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                EntryId = revenueEntry.Id,
                AccountId = accounts["4110"].Id,
                Debit = 0,
                Credit = revenue * 0.7m
            });
            lines.Add(new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                EntryId = revenueEntry.Id,
                AccountId = accounts["4120"].Id,
                Debit = 0,
                Credit = revenue * 0.3m
            });

            entries.Add(revenueEntry);
            entryNo++;

            var expenseEntry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                EntryNo = $"JE-{entryNo:D4}",
                TransactionDate = txDate.AddDays(2),
                Description = $"{period.Name} Operating Expenses",
                Status = "POSTED",
                TotalAmount = expense,
                CreatedBy = adminId,
                TenantId = tenantId,
                CreatedAt = txDate.AddDays(2)
            };

            var breakdown = new (string code, decimal amount)[]
            {
                ("5210", expense * 0.40m),
                ("5100", expense * 0.30m),
                ("5220", expense * 0.10m),
                ("5230", expense * 0.07m),
                ("5240", expense * 0.03m),
                ("5300", expense * 0.10m),
            };

            foreach (var (code, amount) in breakdown)
            {
                lines.Add(new JournalEntryLine
                {
                    Id = Guid.NewGuid(),
                    EntryId = expenseEntry.Id,
                    AccountId = accounts[code].Id,
                    Debit = amount,
                    Credit = 0
                });
            }

            lines.Add(new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                EntryId = expenseEntry.Id,
                AccountId = accounts["1110"].Id,
                Debit = 0,
                Credit = expense
            });

            entries.Add(expenseEntry);
            entryNo++;
        }

        db.AccountingPeriods.AddRange(periods);
        db.JournalEntries.AddRange(entries);
        db.JournalEntryLines.AddRange(lines);

        var openPeriod = periods.LastOrDefault(p => p.Status == "OPEN");
        if (openPeriod != null)
        {
            var budgetAccounts = new (string code, decimal amount, string notes)[]
            {
                ("4110", 750_000_000m, "Monthly product sales target"),
                ("4120", 300_000_000m, "Service revenue projection"),
                ("5100", 420_000_000m, "COGS budget"),
                ("5210", 280_000_000m, "Monthly payroll"),
                ("5220", 55_000_000m, "Office rent"),
                ("5230", 35_000_000m, "Utility estimates"),
                ("5240", 15_000_000m, "Monthly depreciation"),
                ("5300", 15_000_000m, "Miscellaneous expenses"),
            };

            var budgets = budgetAccounts.Select(ba => new Budget
            {
                Id = Guid.NewGuid(),
                AccountId = accounts[ba.code].Id,
                PeriodId = openPeriod.Id,
                PlannedAmount = ba.amount,
                Notes = ba.notes,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            db.Budgets.AddRange(budgets);
        }

        await db.SaveChangesAsync();
    }
}
