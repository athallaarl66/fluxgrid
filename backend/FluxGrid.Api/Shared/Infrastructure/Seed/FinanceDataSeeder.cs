using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class FinanceDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        await ChartOfAccountSeeder.SeedAsync(db, tenantId);
        await AccountingPeriodSeeder.SeedAsync(db, tenantId);

        var accounts = await db.ChartOfAccounts
            .Where(a => a.TenantId == tenantId)
            .ToDictionaryAsync(a => a.Code);

        var periods = await db.AccountingPeriods
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        var year = periods[0].StartDate.Year;

        if (await db.JournalEntries.AnyAsync(e => e.TenantId == tenantId && e.TransactionDate.Year == year))
            return;

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM journal_entry_lines WHERE \"EntryId\" IN (SELECT \"Id\" FROM journal_entries WHERE \"TenantId\" = {0})", tenantId);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM journal_entries WHERE \"TenantId\" = {0}", tenantId);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM budgets WHERE \"TenantId\" = {0}", tenantId);

        var adminId = (await db.Users.FirstOrDefaultAsync(u => u.Username == "admin"))?.Id ?? Guid.Empty;

        var monthlyData = new[]
        {
            (month: 1, revenue: 550_000_000m, expense: 535_000_000m),
            (month: 2, revenue: 580_000_000m, expense: 552_000_000m),
            (month: 3, revenue: 620_000_000m, expense: 574_000_000m),
            (month: 4, revenue: 650_000_000m, expense: 590_000_000m),
            (month: 5, revenue: 690_000_000m, expense: 612_000_000m),
            (month: 6, revenue: 720_000_000m, expense: 633_000_000m),
            (month: 7, revenue: 380_000_000m, expense: 290_000_000m),
            (month: 8, revenue: 560_000_000m, expense: 500_000_000m),
            (month: 9, revenue: 600_000_000m, expense: 520_000_000m),
            (month: 10, revenue: 630_000_000m, expense: 540_000_000m),
            (month: 11, revenue: 660_000_000m, expense: 560_000_000m),
            (month: 12, revenue: 700_000_000m, expense: 580_000_000m),
        };

        var entries = new List<JournalEntry>();
        var lines = new List<JournalEntryLine>();
        var entryNo = 1;

        foreach (var (month, revenue, expense) in monthlyData)
        {
            var period = periods[month - 1];
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
