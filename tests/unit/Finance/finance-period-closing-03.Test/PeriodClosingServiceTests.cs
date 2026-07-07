using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.Finance;

public class PeriodClosingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PeriodService _service;
    private readonly DomainEventDispatcher _events;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PeriodClosingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _events = new DomainEventDispatcher();
        var audit = new AuditService(_db);
        _service = new PeriodService(_db, audit, _events);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── GetListAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetListAsync_ReturnsPeriodsOrderedByStartDateDescending()
    {
        await SeedPeriods();

        var result = await _service.GetListAsync(_tenantId);

        for (int i = 0; i < result.Count - 1; i++)
            Assert.True(result[i].StartDate >= result[i + 1].StartDate);
    }

    [Fact]
    public async Task GetListAsync_ReturnsOnlyTenantPeriods()
    {
        await SeedPeriods();
        var otherTenant = Guid.NewGuid();
        _db.AccountingPeriods.Add(new AccountingPeriod
        {
            Id = Guid.NewGuid(),
            Name = "Other Tenant Period",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Status = "OPEN",
            TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId);

        Assert.All(result, p => Assert.Equal(_tenantId, p.TenantId));
    }

    [Fact]
    public async Task GetListAsync_ReturnsEmptyWhenNoPeriods()
    {
        var result = await _service.GetListAsync(_tenantId);

        Assert.Empty(result);
    }

    // ─── GenerateMissingPeriodsAsync ─────────────────────────────────

    [Fact]
    public async Task GenerateMissingPeriodsAsync_Generates36PeriodsWhenNoneExist()
    {
        var count = await _service.GenerateMissingPeriodsAsync(_tenantId, _userId);

        Assert.Equal(36, count);

        var all = await _db.AccountingPeriods.Where(p => p.TenantId == _tenantId).ToListAsync();
        Assert.Equal(36, all.Count);
    }

    [Fact]
    public async Task GenerateMissingPeriodsAsync_DoesNotDuplicateExistingPeriods()
    {
        await _service.GenerateMissingPeriodsAsync(_tenantId, _userId);

        var count = await _service.GenerateMissingPeriodsAsync(_tenantId, _userId);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GenerateMissingPeriodsAsync_CreatesAuditLog()
    {
        await _service.GenerateMissingPeriodsAsync(_tenantId, _userId);

        var auditLogs = await _db.AuditLogs
            .Where(a => a.TenantId == _tenantId && a.Action == "GENERATE")
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
    }

    // ─── ValidateCloseAsync ─────────────────────────────────────────

    [Fact]
    public async Task ValidateCloseAsync_ReturnsCanCloseTrueForOpenPeriod()
    {
        var period = await CreateOpenPeriod();

        var result = await _service.ValidateCloseAsync(period.Id, _tenantId);

        Assert.True(result.CanClose);
        Assert.Empty(result.BlockingEntryIds);
    }

    [Fact]
    public async Task ValidateCloseAsync_BlocksWhenDraftEntriesExist()
    {
        var period = await CreateOpenPeriod();
        await CreateJournalEntry(period, "DRAFT");

        var result = await _service.ValidateCloseAsync(period.Id, _tenantId);

        Assert.False(result.CanClose);
        Assert.NotEmpty(result.BlockingEntryIds);
    }

    [Fact]
    public async Task ValidateCloseAsync_BlocksWhenPendingApprovalEntriesExist()
    {
        var period = await CreateOpenPeriod();
        await CreateJournalEntry(period, "PENDING_APPROVAL");

        var result = await _service.ValidateCloseAsync(period.Id, _tenantId);

        Assert.False(result.CanClose);
        Assert.NotEmpty(result.BlockingEntryIds);
    }

    [Fact]
    public async Task ValidateCloseAsync_IgnoresPostedEntries()
    {
        var period = await CreateOpenPeriod();
        await CreateJournalEntry(period, "POSTED");

        var result = await _service.ValidateCloseAsync(period.Id, _tenantId);

        Assert.True(result.CanClose);
        Assert.Empty(result.BlockingEntryIds);
    }

    [Fact]
    public async Task ValidateCloseAsync_ThrowsWhenPeriodNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ValidateCloseAsync(Guid.NewGuid(), _tenantId));
    }

    [Fact]
    public async Task ValidateCloseAsync_ThrowsWhenPeriodAlreadyClosed()
    {
        var period = await CreateClosedPeriod();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ValidateCloseAsync(period.Id, _tenantId));
    }

    [Fact]
    public async Task ValidateCloseAsync_ThrowsForWrongTenant()
    {
        var period = await CreateOpenPeriod();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ValidateCloseAsync(period.Id, Guid.NewGuid()));
    }

    // ─── CloseAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_SetsStatusClosed()
    {
        var period = await CreateOpenPeriod();
        var request = new ClosePeriodRequest("CLOSE");

        var result = await _service.CloseAsync(period.Id, _tenantId, request, _userId);

        Assert.Equal("CLOSED", result.Status);
        Assert.Equal(_userId, result.ClosedBy);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task CloseAsync_ThrowsOnInvalidConfirmation()
    {
        var period = await CreateOpenPeriod();
        var request = new ClosePeriodRequest("NOT_CLOSE");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloseAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task CloseAsync_ThrowsWhenPeriodAlreadyClosed()
    {
        var period = await CreateClosedPeriod();
        var request = new ClosePeriodRequest("CLOSE");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloseAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task CloseAsync_ThrowsWhenPeriodNotFound()
    {
        var request = new ClosePeriodRequest("CLOSE");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloseAsync(Guid.NewGuid(), _tenantId, request, _userId));
    }

    [Fact]
    public async Task CloseAsync_ThrowsForWrongTenant()
    {
        var period = await CreateOpenPeriod();
        var request = new ClosePeriodRequest("CLOSE");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloseAsync(period.Id, Guid.NewGuid(), request, _userId));
    }

    [Fact]
    public async Task CloseAsync_ThrowsWhenBlockingEntriesExist()
    {
        var period = await CreateOpenPeriod();
        await CreateJournalEntry(period, "DRAFT");
        var request = new ClosePeriodRequest("CLOSE");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloseAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task CloseAsync_RaisesPeriodClosedEvent()
    {
        var period = await CreateOpenPeriod();
        var request = new ClosePeriodRequest("CLOSE");

        await _service.CloseAsync(period.Id, _tenantId, request, _userId);

        var raised = _events.GetEvents().OfType<PeriodClosed>().ToList();
        Assert.Single(raised);
        Assert.Equal(period.Id, raised[0].PeriodId);
        Assert.Equal(_userId, raised[0].ClosedBy);
        Assert.Equal(_tenantId, raised[0].TenantId);
    }

    // ─── ReopenAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ReopenAsync_SetsStatusOpen()
    {
        var period = await CreateClosedPeriod();
        var request = new ReopenPeriodRequest("Need to correct entries");

        var result = await _service.ReopenAsync(period.Id, _tenantId, request, _userId);

        Assert.Equal("OPEN", result.Status);
        Assert.Null(result.ClosedBy);
        Assert.Null(result.ClosedAt);
    }

    [Fact]
    public async Task ReopenAsync_ThrowsOnEmptyReason()
    {
        var period = await CreateClosedPeriod();
        var request = new ReopenPeriodRequest("");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReopenAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task ReopenAsync_ThrowsOnShortReason()
    {
        var period = await CreateClosedPeriod();
        var request = new ReopenPeriodRequest("Short");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReopenAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task ReopenAsync_ThrowsOnWhitespaceReason()
    {
        var period = await CreateClosedPeriod();
        var request = new ReopenPeriodRequest("     ");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReopenAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task ReopenAsync_ThrowsWhenPeriodAlreadyOpen()
    {
        var period = await CreateOpenPeriod();
        var request = new ReopenPeriodRequest("Need to correct entries");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReopenAsync(period.Id, _tenantId, request, _userId));
    }

    [Fact]
    public async Task ReopenAsync_ThrowsWhenPeriodNotFound()
    {
        var request = new ReopenPeriodRequest("Need to correct entries");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReopenAsync(Guid.NewGuid(), _tenantId, request, _userId));
    }

    [Fact]
    public async Task ReopenAsync_ThrowsForWrongTenant()
    {
        var period = await CreateClosedPeriod();
        var request = new ReopenPeriodRequest("Need to correct entries");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ReopenAsync(period.Id, Guid.NewGuid(), request, _userId));
    }

    [Fact]
    public async Task ReopenAsync_RaisesPeriodReopenedEvent()
    {
        var period = await CreateClosedPeriod();
        var reason = "Need to correct entries";
        var request = new ReopenPeriodRequest(reason);

        await _service.ReopenAsync(period.Id, _tenantId, request, _userId);

        var raised = _events.GetEvents().OfType<PeriodReopened>().ToList();
        Assert.Single(raised);
        Assert.Equal(period.Id, raised[0].PeriodId);
        Assert.Equal(reason, raised[0].Reason);
        Assert.Equal(_userId, raised[0].ReopenedBy);
        Assert.Equal(_tenantId, raised[0].TenantId);
    }

    // ─── Helper Methods ─────────────────────────────────────────────

    private async Task<AccountingPeriod> CreateOpenPeriod()
    {
        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(),
            Name = "January 2026",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Status = "OPEN",
            TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);
        await _db.SaveChangesAsync();
        return period;
    }

    private async Task<AccountingPeriod> CreateClosedPeriod()
    {
        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(),
            Name = "December 2025",
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2025, 12, 31),
            Status = "CLOSED",
            ClosedBy = _userId,
            ClosedAt = DateTime.UtcNow,
            TenantId = _tenantId
        };
        _db.AccountingPeriods.Add(period);
        await _db.SaveChangesAsync();
        return period;
    }

    private async Task SeedPeriods()
    {
        for (int month = 1; month <= 6; month++)
        {
            _db.AccountingPeriods.Add(new AccountingPeriod
            {
                Id = Guid.NewGuid(),
                Name = $"2026-{month:D2}",
                StartDate = new DateTime(2026, month, 1),
                EndDate = new DateTime(2026, month, 1).AddMonths(1).AddDays(-1),
                Status = "OPEN",
                TenantId = _tenantId
            });
        }
        await _db.SaveChangesAsync();
    }

    private async Task CreateJournalEntry(AccountingPeriod period, string status)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = period.StartDate.AddDays(1),
            Description = $"Test entry ({status})",
            Status = status,
            TotalAmount = 100000,
            CreatedBy = _userId,
            TenantId = _tenantId
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
    }
}
