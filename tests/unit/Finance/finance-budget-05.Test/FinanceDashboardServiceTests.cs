using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.Finance;

public class FinanceDashboardServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FinanceDashboardService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public FinanceDashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new FinanceDashboardService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedPeriodAndAccounts()
    {
        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Jan 2025",
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);

        var asset = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "1000", Name = "Assets", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId };
        var liability = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "2000", Name = "Liabilities", Type = AccountTypes.Liability, IsActive = true, TenantId = _tenantId };
        var equity = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "3000", Name = "Equity", Type = AccountTypes.Equity, IsActive = true, TenantId = _tenantId };
        var revenue = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "4000", Name = "Revenue", Type = AccountTypes.Revenue, IsActive = true, TenantId = _tenantId };
        var expense = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "5000", Name = "Expenses", Type = AccountTypes.Expense, IsActive = true, TenantId = _tenantId };

        _db.ChartOfAccounts.AddRange(asset, liability, equity, revenue, expense);
        await _db.SaveChangesAsync();
    }

    private async Task<JournalEntry> SeedPostedEntry(Guid accountId, decimal debit, decimal credit, DateTime date)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = "JE-" + Guid.NewGuid().ToString("N")[..6],
            TransactionDate = date,
            Description = "Test entry",
            Status = "POSTED",
            TotalAmount = debit + credit,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            AccountId = accountId,
            Debit = debit,
            Credit = credit,
        };
        _db.JournalEntries.Add(entry);
        _db.JournalEntryLines.Add(line);
        await _db.SaveChangesAsync();
        return entry;
    }

    // ─── GetDashboardAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetDashboardAsync_ReturnsZero_WhenNoPeriod()
    {
        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(0m, result.TotalAssets);
        Assert.Equal(0, result.JournalEntryCount);
        Assert.Equal(Guid.Empty, result.PeriodId);
        Assert.Empty(result.RecentEntries);
        Assert.Empty(result.MonthlyTrend);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsKpis_WithData()
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Current Year",
            StartDate = periodStart, EndDate = periodEnd,
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);

        var asset = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "1000", Name = "Assets", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId };
        var liability = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "2000", Name = "Liabilities", Type = AccountTypes.Liability, IsActive = true, TenantId = _tenantId };
        var equity = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "3000", Name = "Equity", Type = AccountTypes.Equity, IsActive = true, TenantId = _tenantId };
        var revenue = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "4000", Name = "Revenue", Type = AccountTypes.Revenue, IsActive = true, TenantId = _tenantId };
        var expense = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "5000", Name = "Expenses", Type = AccountTypes.Expense, IsActive = true, TenantId = _tenantId };

        _db.ChartOfAccounts.AddRange(asset, liability, equity, revenue, expense);
        await _db.SaveChangesAsync();

        var midDate = new DateTime(now.Year, now.Month, Math.Min(15, 28), 0, 0, 0, DateTimeKind.Utc);

        // Asset: debit 100M
        await SeedPostedEntry(asset.Id, 100_000_000m, 0m, midDate);
        // Liability: credit 40M
        await SeedPostedEntry(liability.Id, 0m, 40_000_000m, midDate);
        // Equity: credit 60M
        await SeedPostedEntry(equity.Id, 0m, 60_000_000m, midDate);
        // Revenue: credit 50M
        await SeedPostedEntry(revenue.Id, 0m, 50_000_000m, midDate);
        // Expense: debit 30M
        await SeedPostedEntry(expense.Id, 30_000_000m, 0m, midDate);

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(100_000_000m, result.TotalAssets);
        Assert.Equal(40_000_000m, result.TotalLiabilities);
        Assert.Equal(60_000_000m, result.TotalEquity);
        Assert.Equal(50_000_000m, result.RevenueMtd);
        Assert.Equal(30_000_000m, result.ExpensesMtd);
        Assert.Equal(20_000_000m, result.NetIncomeMtd);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsRecentEntries()
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Current Year",
            StartDate = periodStart, EndDate = periodEnd,
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);

        var expense = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "5000", Name = "Expenses", Type = AccountTypes.Expense, IsActive = true, TenantId = _tenantId };
        _db.ChartOfAccounts.Add(expense);
        await _db.SaveChangesAsync();

        for (int i = 0; i < 15; i++)
        {
            await SeedPostedEntry(expense.Id, 1_000_000m, 0m,
                new DateTime(now.Year, now.Month, Math.Min(i + 1, 28), 0, 0, 0, DateTimeKind.Utc));
        }

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(10, result.RecentEntries.Count);
        Assert.True(result.RecentEntries[0].TransactionDate >= result.RecentEntries[^1].TransactionDate);
    }

    [Fact]
    public async Task GetDashboardAsync_ExcludesDraftEntries()
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Current Year",
            StartDate = periodStart, EndDate = periodEnd,
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);

        var revenue = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "4000", Name = "Revenue", Type = AccountTypes.Revenue, IsActive = true, TenantId = _tenantId };
        _db.ChartOfAccounts.Add(revenue);
        await _db.SaveChangesAsync();

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = "JE-DRAFT",
            TransactionDate = new DateTime(now.Year, now.Month, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Draft entry",
            Status = "DRAFT",
            TotalAmount = 100_000_000m,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(), EntryId = entry.Id,
            AccountId = revenue.Id, Debit = 0, Credit = 100_000_000m,
        };
        _db.JournalEntries.Add(entry);
        _db.JournalEntryLines.Add(line);
        await _db.SaveChangesAsync();

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(0m, result.RevenueMtd);
        Assert.Empty(result.RecentEntries);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsMonthlyTrend()
    {
        await SeedPeriodAndAccounts();
        var accounts = await _db.ChartOfAccounts.ToListAsync();
        var revenue = accounts.First(a => a.Type == AccountTypes.Revenue);
        var expense = accounts.First(a => a.Type == AccountTypes.Expense);

        var now = DateTime.UtcNow;
        var midDate = new DateTime(now.Year, now.Month, Math.Min(15, 28), 0, 0, 0, DateTimeKind.Utc);

        await SeedPostedEntry(revenue.Id, 0m, 10_000_000m, midDate);
        await SeedPostedEntry(expense.Id, 4_000_000m, 0m, midDate);

        var result = await _service.GetDashboardAsync(_tenantId);

        var trend = result.MonthlyTrend.FirstOrDefault(t => t.Month == now.Month);
        Assert.NotNull(trend);
        Assert.Equal(10_000_000m, trend.Revenue);
        Assert.Equal(4_000_000m, trend.Expenses);
    }

    [Fact]
    public async Task GetDashboardAsync_RespectsTenantIsolation()
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Current Year",
            StartDate = periodStart, EndDate = periodEnd,
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);

        var revenue = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "4000", Name = "Revenue", Type = AccountTypes.Revenue, IsActive = true, TenantId = _tenantId };
        _db.ChartOfAccounts.Add(revenue);
        await _db.SaveChangesAsync();

        var midDate = new DateTime(now.Year, now.Month, Math.Min(15, 28), 0, 0, 0, DateTimeKind.Utc);
        await SeedPostedEntry(revenue.Id, 0m, 50_000_000m, midDate);

        var otherTenant = Guid.NewGuid();
        var result = await _service.GetDashboardAsync(otherTenant);

        Assert.Equal(0m, result.RevenueMtd);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsJournalEntryCount()
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Current Year",
            StartDate = periodStart, EndDate = periodEnd,
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);

        var expense = new ChartOfAccount
        { Id = Guid.NewGuid(), Code = "5000", Name = "Expenses", Type = AccountTypes.Expense, IsActive = true, TenantId = _tenantId };
        _db.ChartOfAccounts.Add(expense);
        await _db.SaveChangesAsync();

        var mid1 = new DateTime(now.Year, now.Month, 10, 0, 0, 0, DateTimeKind.Utc);
        var mid2 = new DateTime(now.Year, now.Month, 20, 0, 0, 0, DateTimeKind.Utc);
        var mid3 = new DateTime(now.Year, now.Month, 25, 0, 0, 0, DateTimeKind.Utc);

        await SeedPostedEntry(expense.Id, 1_000_000m, 0m, mid1);
        await SeedPostedEntry(expense.Id, 2_000_000m, 0m, mid2);
        await SeedPostedEntry(expense.Id, 3_000_000m, 0m, mid3);

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(3, result.JournalEntryCount);
    }
}
