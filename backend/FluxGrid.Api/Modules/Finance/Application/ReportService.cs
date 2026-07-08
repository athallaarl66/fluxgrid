using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class ReportService
{
    private static readonly HashSet<string> NormalDebitTypes = [AccountTypes.Asset, AccountTypes.Expense];

    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportResponse> GetTrialBalanceAsync(Guid tenantId, DateTime startDate, DateTime endDate, bool includeDrafts)
    {
        var rows = await GetAggregatedRowsAsync(tenantId, ToUtc(startDate), ToUtc(endDate), includeDrafts, null);
        var tree = BuildTree(rows);
        var totalDebit = Sum(tree, r => r.Debit);
        var totalCredit = Sum(tree, r => r.Credit);
        return new ReportResponse(tree, totalDebit, totalCredit, null);
    }

    public async Task<ReportResponse> GetProfitLossAsync(Guid tenantId, DateTime startDate, DateTime endDate, bool includeDrafts)
    {
        var rows = await GetAggregatedRowsAsync(tenantId, ToUtc(startDate), ToUtc(endDate), includeDrafts, [AccountTypes.Revenue, AccountTypes.Expense]);
        var tree = BuildTree(rows);
        var totalDebit = Sum(tree, r => r.Debit);
        var totalCredit = Sum(tree, r => r.Credit);
        var revenue = SumByType(tree, AccountTypes.Revenue);
        var expense = SumByType(tree, AccountTypes.Expense);
        var netIncome = revenue - expense;
        return new ReportResponse(tree, totalDebit, totalCredit, netIncome);
    }

    public async Task<ReportResponse> GetBalanceSheetAsync(Guid tenantId, DateTime asOfDate, bool includeDrafts, decimal? netIncome)
    {
        var rows = await GetAggregatedRowsAsync(tenantId, ToUtc(DateTime.MinValue), ToUtc(asOfDate), includeDrafts, [AccountTypes.Asset, AccountTypes.Liability, AccountTypes.Equity]);

        if (netIncome.HasValue)
        {
            var topEquity = rows.FirstOrDefault(r => r.Type == AccountTypes.Equity && r.ParentId == null);
            if (topEquity != null)
            {
                var cyeDebit = netIncome.Value < 0 ? -netIncome.Value : 0m;
                var cyeCredit = netIncome.Value > 0 ? netIncome.Value : 0m;
                rows.Add(new FlatReportRow(Guid.Empty, "CYE", "Current Year Earnings", AccountTypes.Equity, topEquity.Id, cyeDebit, cyeCredit));
            }
        }

        var tree = BuildTree(rows);
        var totalDebit = Sum(tree, r => r.Debit);
        var totalCredit = Sum(tree, r => r.Credit);
        return new ReportResponse(tree, totalDebit, totalCredit, netIncome);
    }

    public async Task<(List<LedgerDetailRow> Rows, int Total)> GetAccountLedgerAsync(
        Guid accountId, Guid tenantId, DateTime startDate, DateTime endDate, bool includeDrafts, int page, int pageSize)
    {
        startDate = ToUtc(startDate);
        endDate = ToUtc(endDate);
        var query = from je in _db.JournalEntries
                    join jel in _db.JournalEntryLines on je.Id equals jel.EntryId
                    where jel.AccountId == accountId
                          && je.TenantId == tenantId
                          && je.TransactionDate >= startDate
                          && je.TransactionDate <= endDate
                          && (includeDrafts || je.Status == "POSTED" || je.Status == "APPROVED")
                    orderby je.TransactionDate descending
                    select new LedgerDetailRow(
                        je.Id,
                        je.EntryNo,
                        je.TransactionDate,
                        je.Description,
                        jel.Debit,
                        jel.Credit,
                        je.CreatedAt
                    );

        var total = await query.CountAsync();
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (rows, total);
    }

    private async Task<List<FlatReportRow>> GetAggregatedRowsAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, bool includeDrafts, string[]? accountTypes)
    {
        var accountsQuery = _db.ChartOfAccounts
            .Where(a => a.TenantId == tenantId && a.IsActive);

        if (accountTypes != null)
            accountsQuery = accountsQuery.Where(a => accountTypes.Contains(a.Type));

        var accounts = await accountsQuery.OrderBy(a => a.Code).ToListAsync();
        var accountIds = accounts.Select(a => a.Id).ToHashSet();

        var aggregation = await (
            from jel in _db.JournalEntryLines
            join je in _db.JournalEntries on jel.EntryId equals je.Id
            where accountIds.Contains(jel.AccountId)
                  && je.TenantId == tenantId
                  && je.TransactionDate >= startDate
                  && je.TransactionDate <= endDate
                  && (includeDrafts || je.Status == "POSTED" || je.Status == "APPROVED")
            group new { jel.Debit, jel.Credit } by jel.AccountId into g
            select new { AccountId = g.Key, TotalDebit = g.Sum(x => x.Debit), TotalCredit = g.Sum(x => x.Credit) }
        ).ToDictionaryAsync(x => x.AccountId);

        return accounts.Select(a =>
        {
            aggregation.TryGetValue(a.Id, out var agg);
            return new FlatReportRow(a.Id, a.Code, a.Name, a.Type, a.ParentId, agg?.TotalDebit ?? 0m, agg?.TotalCredit ?? 0m);
        }).ToList();
    }

    private static List<ReportRow> BuildTree(List<FlatReportRow> flatRows)
    {
        var lookup = flatRows.ToLookup(r => r.ParentId);
        return BuildLevel(lookup, null, 0);
    }

    private static List<ReportRow> BuildLevel(ILookup<Guid?, FlatReportRow> lookup, Guid? parentId, int depth)
    {
        return lookup[parentId].OrderBy(r => r.Code).Select(r =>
        {
            var children = BuildLevel(lookup, r.Id, depth + 1);
            var childDebit = children.Sum(c => c.Debit);
            var childCredit = children.Sum(c => c.Credit);
            var totalDebit = r.Debit + childDebit;
            var totalCredit = r.Credit + childCredit;
            var balance = NormalDebitTypes.Contains(r.Type) ? totalDebit - totalCredit : totalCredit - totalDebit;
            return new ReportRow(r.Id, r.Code, r.Name, r.Type, depth, totalDebit, totalCredit, balance, children);
        }).ToList();
    }

    private static decimal Sum(List<ReportRow> rows, Func<ReportRow, decimal> selector)
    {
        var total = 0m;
        foreach (var row in rows)
            total += selector(row);
        return total;
    }

    private static decimal SumByType(List<ReportRow> rows, string type)
    {
        var total = 0m;
        foreach (var row in rows)
            if (row.Type == type)
                total += row.Balance;
        return total;
    }

    private static DateTime ToUtc(DateTime dt) => DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    private record FlatReportRow(Guid Id, string Code, string Name, string Type, Guid? ParentId, decimal Debit, decimal Credit);
}
