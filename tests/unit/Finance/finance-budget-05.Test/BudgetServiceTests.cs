using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.Finance;

public class BudgetServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BudgetService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private Guid _accountId;
    private Guid _periodId;

    public BudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        var audit = new AuditService(_db);
        _service = new BudgetService(_db, audit);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedBaseData(bool closePeriod = false)
    {
        var account = new ChartOfAccount
        {
            Id = Guid.NewGuid(), Code = "4110", Name = "Product Sales",
            Type = "REVENUE", IsActive = true, TenantId = _tenantId
        };
        _accountId = account.Id;

        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Jan 2025",
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            Status = closePeriod ? "CLOSED" : "OPEN",
            TenantId = _tenantId
        };
        _periodId = period.Id;

        _db.ChartOfAccounts.Add(account);
        _db.AccountingPeriods.Add(period);
        await _db.SaveChangesAsync();
    }

    private async Task<List<Budget>> SeedBudgets(int count = 3)
    {
        var budgets = Enumerable.Range(1, count).Select(i => new Budget
        {
            Id = Guid.NewGuid(),
            AccountId = _accountId,
            PeriodId = _periodId,
            PlannedAmount = i * 1_000_000m,
            Notes = i == 1 ? "First budget" : null,
            TenantId = _tenantId
        }).ToList();

        _db.Budgets.AddRange(budgets);
        await _db.SaveChangesAsync();
        return budgets;
    }

    // ─── GetListAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetListAsync_ReturnsPaginatedBudgets()
    {
        await SeedBaseData();
        await SeedBudgets(5);

        var result = await _service.GetListAsync(_tenantId, null, null, 1, 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetListAsync_FiltersByPeriod()
    {
        await SeedBaseData();
        await SeedBudgets();

        var otherPeriod = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Feb 2025",
            StartDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 2, 28, 23, 59, 59, DateTimeKind.Utc),
            Status = "OPEN", TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(otherPeriod);
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, otherPeriod.Id, null, 1, 10);

        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetListAsync_ReturnsEmpty_WhenNoBudgets()
    {
        await SeedBaseData();

        var result = await _service.GetListAsync(_tenantId, null, null, 1, 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetListAsync_RespectsPageDefaults()
    {
        await SeedBaseData();
        await SeedBudgets(3);

        var result = await _service.GetListAsync(_tenantId, null, null, -1, -1);

        Assert.NotEmpty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    // ─── CreateAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesBudgetSuccessfully()
    {
        await SeedBaseData();
        var request = new CreateBudgetRequest(_accountId, _periodId, 50_000_000m, "Test budget");

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal(50_000_000m, result.PlannedAmount);
        Assert.Equal("Test budget", result.Notes);
        Assert.Equal("4110", result.AccountCode);
        Assert.Equal("Jan 2025", result.PeriodName);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenAccountNotFound()
    {
        await SeedBaseData();
        var request = new CreateBudgetRequest(Guid.NewGuid(), _periodId, 10_000_000m, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, _userId));
        Assert.Contains("Account not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenAccountInactive()
    {
        await SeedBaseData();
        var account = await _db.ChartOfAccounts.FindAsync(_accountId);
        account!.IsActive = false;
        await _db.SaveChangesAsync();

        var request = new CreateBudgetRequest(_accountId, _periodId, 10_000_000m, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, _userId));
        Assert.Contains("not active", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPeriodNotFound()
    {
        await SeedBaseData();
        var request = new CreateBudgetRequest(_accountId, Guid.NewGuid(), 10_000_000m, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, _userId));
        Assert.Contains("Period not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPeriodClosed()
    {
        await SeedBaseData(closePeriod: true);
        var request = new CreateBudgetRequest(_accountId, _periodId, 10_000_000m, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, _userId));
        Assert.Contains("closed period", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDuplicateExists()
    {
        await SeedBaseData();
        var request = new CreateBudgetRequest(_accountId, _periodId, 10_000_000m, null);
        await _service.CreateAsync(_tenantId, request, _userId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, _userId));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsSameAccountDifferentTenant()
    {
        await SeedBaseData();
        var otherTenant = Guid.NewGuid();
        var account = new ChartOfAccount
        {
            Id = Guid.NewGuid(), Code = "4110", Name = "Product Sales",
            Type = "REVENUE", IsActive = true, TenantId = otherTenant
        };
        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Jan 2025",
            StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            Status = "OPEN", TenantId = otherTenant
        };
        _db.ChartOfAccounts.Add(account);
        _db.AccountingPeriods.Add(period);
        await _db.SaveChangesAsync();

        var request = new CreateBudgetRequest(_accountId, _periodId, 10_000_000m, null);
        var result = await _service.CreateAsync(otherTenant, new CreateBudgetRequest(account.Id, period.Id, 10_000_000m, null), _userId);

        Assert.NotNull(result);
        Assert.Equal(10_000_000m, result.PlannedAmount);
    }

    // ─── UpdateAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesPlannedAmount()
    {
        await SeedBaseData();
        var budgets = await SeedBudgets(1);
        var request = new UpdateBudgetRequest(20_000_000m, null);

        var result = await _service.UpdateAsync(budgets[0].Id, _tenantId, request, _userId);

        Assert.Equal(20_000_000m, result.PlannedAmount);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNotes()
    {
        await SeedBaseData();
        var budgets = await SeedBudgets(1);
        var request = new UpdateBudgetRequest(null, "Updated notes");

        var result = await _service.UpdateAsync(budgets[0].Id, _tenantId, request, _userId);

        Assert.Equal("Updated notes", result.Notes);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenBudgetNotFound()
    {
        var request = new UpdateBudgetRequest(10_000_000m, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), _tenantId, request, _userId));
        Assert.Contains("not found", ex.Message);
    }

    // ─── DeleteAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesBudget()
    {
        await SeedBaseData();
        var budgets = await SeedBudgets(1);

        await _service.DeleteAsync(budgets[0].Id, _tenantId, _userId);

        var remaining = await _db.Budgets.CountAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenBudgetNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync(Guid.NewGuid(), _tenantId, _userId));
    }

    // ─── GetBudgetVsActualAsync ──────────────────────────────────────

    [Fact]
    public async Task GetBudgetVsActualAsync_ReturnsEmpty_WhenNoBudgets()
    {
        await SeedBaseData();

        var result = await _service.GetBudgetVsActualAsync(_tenantId, _periodId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBudgetVsActualAsync_ReturnsVarianceRow_WithFlag()
    {
        await SeedBaseData();
        var budgets = await SeedBudgets(1);
        var budget = budgets[0];
        // budget.PlannedAmount = 1_000_000

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = "JE-001",
            TransactionDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Test entry",
            Status = "POSTED",
            TotalAmount = 1_400_000m,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            AccountId = _accountId,
            Debit = 1_400_000m,
            Credit = 0,
            Description = "Debit line"
        };
        _db.JournalEntries.Add(entry);
        _db.JournalEntryLines.Add(line);
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetVsActualAsync(_tenantId, _periodId);

        Assert.Single(result);
        Assert.Equal("4110", result[0].AccountCode);
        Assert.Equal(1_000_000m, result[0].PlannedAmount);
        Assert.Equal(1_400_000m, result[0].ActualAmount);
        Assert.Equal(-400_000m, result[0].Variance);
        Assert.Equal(-40m, result[0].VariancePercentage);
        Assert.True(result[0].IsFlagged);
    }

    [Fact]
    public async Task GetBudgetVsActualAsync_NotFlagged_WhenUnderThreshold()
    {
        await SeedBaseData();
        var budgets = await SeedBudgets(1);
        var budget = budgets[0];
        // budget.PlannedAmount = 1_000_000, actual = 1_100_000 => 10% variance, < 20% threshold

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = "JE-001",
            TransactionDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Test entry",
            Status = "POSTED",
            TotalAmount = 1_100_000m,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            AccountId = _accountId,
            Debit = 1_100_000m,
            Credit = 0,
        };
        _db.JournalEntries.Add(entry);
        _db.JournalEntryLines.Add(line);
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetVsActualAsync(_tenantId, _periodId);

        Assert.False(result[0].IsFlagged);
    }

    [Fact]
    public async Task GetBudgetVsActualAsync_ExcludesDraftEntries()
    {
        await SeedBaseData();
        await SeedBudgets(1);

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = "JE-002",
            TransactionDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Draft entry",
            Status = "DRAFT",
            TotalAmount = 5_000_000m,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            AccountId = _accountId,
            Debit = 5_000_000m,
            Credit = 0,
        };
        _db.JournalEntries.Add(entry);
        _db.JournalEntryLines.Add(line);
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetVsActualAsync(_tenantId, _periodId);

        Assert.Equal(0m, result[0].ActualAmount);
    }

    [Fact]
    public async Task GetBudgetVsActualAsync_IncludesApprovedEntries()
    {
        await SeedBaseData();
        await SeedBudgets(1);

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = "JE-003",
            TransactionDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Description = "Approved entry",
            Status = "APPROVED",
            TotalAmount = 500_000m,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        var line = new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            AccountId = _accountId,
            Debit = 500_000m,
            Credit = 0,
        };
        _db.JournalEntries.Add(entry);
        _db.JournalEntryLines.Add(line);
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetVsActualAsync(_tenantId, _periodId);

        Assert.Equal(500_000m, result[0].ActualAmount);
    }

    [Fact]
    public async Task GetBudgetVsActualAsync_ZeroPlannedAmount_DoesNotDivideByZero()
    {
        await SeedBaseData();
        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            AccountId = _accountId,
            PeriodId = _periodId,
            PlannedAmount = 0m,
            TenantId = _tenantId
        };
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();

        var result = await _service.GetBudgetVsActualAsync(_tenantId, _periodId);

        Assert.Single(result);
        Assert.Equal(0m, result[0].VariancePercentage);
    }

    [Fact]
    public async Task GetBudgetVsActualAsync_RespectsTenantIsolation()
    {
        await SeedBaseData();
        await SeedBudgets(1);
        var otherTenant = Guid.NewGuid();

        var result = await _service.GetBudgetVsActualAsync(otherTenant, _periodId);

        Assert.Empty(result);
    }
}
