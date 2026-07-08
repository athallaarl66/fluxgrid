using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class FinanceDashboardService
{
    private readonly AppDbContext _db;

    public FinanceDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid tenantId)
    {
        var currentPeriod = await _db.AccountingPeriods
            .Where(p => p.TenantId == tenantId && p.Status == "OPEN")
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        if (currentPeriod is null)
            return new DashboardResponse(0, 0, 0, 0, 0, 0, 0, Guid.Empty, [], []);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var accountBalances = await (
            from jel in _db.JournalEntryLines
            join je in _db.JournalEntries on jel.EntryId equals je.Id
            join ac in _db.ChartOfAccounts on jel.AccountId equals ac.Id
            where ac.TenantId == tenantId
                  && je.TenantId == tenantId
                  && (je.Status == "POSTED" || je.Status == "APPROVED")
                  && je.TransactionDate >= currentPeriod.StartDate
                  && je.TransactionDate <= currentPeriod.EndDate
            group new { jel.Debit, jel.Credit, ac.Type } by ac.Type into g
            select new { Type = g.Key, TotalDebit = g.Sum(x => x.Debit), TotalCredit = g.Sum(x => x.Credit) }
        ).ToListAsync();

        var totalAssets = accountBalances
            .Where(a => a.Type == AccountTypes.Asset)
            .Sum(a => a.TotalDebit - a.TotalCredit);

        var totalLiabilities = accountBalances
            .Where(a => a.Type == AccountTypes.Liability)
            .Sum(a => a.TotalCredit - a.TotalDebit);

        var totalEquity = accountBalances
            .Where(a => a.Type == AccountTypes.Equity)
            .Sum(a => a.TotalCredit - a.TotalDebit);

        var mtdBalances = await (
            from jel in _db.JournalEntryLines
            join je in _db.JournalEntries on jel.EntryId equals je.Id
            join ac in _db.ChartOfAccounts on jel.AccountId equals ac.Id
            where ac.TenantId == tenantId
                  && je.TenantId == tenantId
                  && (je.Status == "POSTED" || je.Status == "APPROVED")
                  && je.TransactionDate >= monthStart
            group new { jel.Debit, jel.Credit, ac.Type } by ac.Type into g
            select new { Type = g.Key, TotalDebit = g.Sum(x => x.Debit), TotalCredit = g.Sum(x => x.Credit) }
        ).ToListAsync();

        var revenueMtd = mtdBalances
            .Where(a => a.Type == AccountTypes.Revenue)
            .Sum(a => a.TotalCredit - a.TotalDebit);

        var expensesMtd = mtdBalances
            .Where(a => a.Type == AccountTypes.Expense)
            .Sum(a => a.TotalDebit - a.TotalCredit);

        var netIncomeMtd = revenueMtd - expensesMtd;

        var entryCount = await _db.JournalEntries
            .CountAsync(je => je.TenantId == tenantId
                && je.TransactionDate >= currentPeriod.StartDate
                && je.TransactionDate <= currentPeriod.EndDate
                && (je.Status == "POSTED" || je.Status == "APPROVED"));

        var recentEntries = await _db.JournalEntries
            .Where(je => je.TenantId == tenantId
                && (je.Status == "POSTED" || je.Status == "APPROVED"))
            .OrderByDescending(je => je.TransactionDate)
            .Take(10)
            .Select(je => new RecentEntryRow(
                je.Id,
                je.EntryNo,
                je.Description,
                je.TransactionDate,
                je.Lines.Sum(l => l.Debit),
                je.Lines.Sum(l => l.Credit),
                je.Status
            ))
            .ToListAsync();

        var monthlyTrend = await (
            from jel in _db.JournalEntryLines
            join je in _db.JournalEntries on jel.EntryId equals je.Id
            join ac in _db.ChartOfAccounts on jel.AccountId equals ac.Id
            where ac.TenantId == tenantId
                  && je.TenantId == tenantId
                  && (je.Status == "POSTED" || je.Status == "APPROVED")
                  && je.TransactionDate >= yearStart
            group new { jel.Debit, jel.Credit, ac.Type, Month = je.TransactionDate.Month } by new { je.TransactionDate.Month, ac.Type } into g
            select new { g.Key.Month, g.Key.Type, TotalDebit = g.Sum(x => x.Debit), TotalCredit = g.Sum(x => x.Credit) }
        ).ToListAsync();

        var trend = Enumerable.Range(1, now.Month).Select(m =>
        {
            var revenue = monthlyTrend
                .Where(t => t.Month == m && t.Type == AccountTypes.Revenue)
                .Sum(t => t.TotalCredit - t.TotalDebit);
            var expenses = monthlyTrend
                .Where(t => t.Month == m && t.Type == AccountTypes.Expense)
                .Sum(t => t.TotalDebit - t.TotalCredit);
            return new MonthlyTrendRow(m, revenue, expenses);
        }).ToList();

        return new DashboardResponse(
            Math.Max(totalAssets, 0),
            Math.Max(totalLiabilities, 0),
            Math.Max(totalEquity, 0),
            Math.Max(revenueMtd, 0),
            Math.Max(expensesMtd, 0),
            netIncomeMtd,
            entryCount,
            currentPeriod.Id,
            recentEntries,
            trend
        );
    }
}
