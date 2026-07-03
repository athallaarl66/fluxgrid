using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.Finance;

public class JournalEntryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly JournalEntryService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public JournalEntryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new JournalEntryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private List<JournalEntryLineDto> CreateBalancedLines(decimal amount = 1000000)
    {
        return
        [
            new(Guid.NewGuid(), amount, 0, "Debit entry"),
            new(Guid.NewGuid(), 0, amount, "Credit entry")
        ];
    }

    private List<JournalEntryLineDto> CreateUnbalancedLines()
    {
        return
        [
            new(Guid.NewGuid(), 1000000, 0, "Debit only"),
            new(Guid.NewGuid(), 0, 500000, "Partial credit")
        ];
    }

    // ─── CreateAsync: Draft Tests ───────────────────────────────────

    [Fact]
    public async Task CreateAsync_AsDraft_AllowsUnbalancedEntries()
    {
        var lines = CreateUnbalancedLines();
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Test draft",
            lines,
            "DRAFT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal("DRAFT", result.Status);
        Assert.Equal(1000000, result.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_AsDraft_AcceptsBalancedEntries()
    {
        var lines = CreateBalancedLines();
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Balanced draft",
            lines,
            "DRAFT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal("DRAFT", result.Status);
    }

    // ─── CreateAsync: Submit Tests ──────────────────────────────────

    [Fact]
    public async Task CreateAsync_AsSubmit_RejectsUnbalancedEntries()
    {
        var lines = CreateUnbalancedLines();
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Unbalanced submit",
            lines,
            "SUBMIT"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, _userId));
    }

    [Fact]
    public async Task CreateAsync_AsSubmit_BelowThreshold_SetsStatusPosted()
    {
        var lines = CreateBalancedLines(40000000); // Below 50M threshold
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Small submit",
            lines,
            "SUBMIT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal("POSTED", result.Status);
        Assert.Equal(40000000, result.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_AsSubmit_AtThreshold_SetsStatusPosted()
    {
        var lines = CreateBalancedLines(50000000); // Exactly at 50M threshold
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Threshold submit",
            lines,
            "SUBMIT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal("POSTED", result.Status);
    }

    [Fact]
    public async Task CreateAsync_AsSubmit_AboveThreshold_SetsStatusPendingApproval()
    {
        var lines = CreateBalancedLines(60000000); // Above 50M threshold
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Large submit",
            lines,
            "SUBMIT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal("PENDING_APPROVAL", result.Status);
        Assert.Equal(60000000, result.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_SetsCorrectEntryNo()
    {
        var lines = CreateBalancedLines();
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "Entry with number",
            lines,
            "DRAFT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.StartsWith("JE-", result.EntryNo);
        Assert.NotEmpty(result.EntryNo);
    }

    [Fact]
    public async Task CreateAsync_SetsCorrectTenantAndUser()
    {
        var lines = CreateBalancedLines();
        var request = new CreateJournalEntryRequest(
            DateTime.UtcNow,
            "User tracking",
            lines,
            "DRAFT"
        );

        var result = await _service.CreateAsync(_tenantId, request, _userId);

        Assert.Equal(_tenantId, result.TenantId);
        Assert.Equal(_userId, result.CreatedBy);
    }

    // ─── GetListAsync Tests ─────────────────────────────────────────

    [Fact]
    public async Task GetListAsync_ReturnsAllEntriesForTenant()
    {
        await SeedTestEntries();
        var result = await _service.GetListAsync(_tenantId, null, 1, 20);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetListAsync_FiltersByStatus()
    {
        await SeedTestEntries();
        var result = await _service.GetListAsync(_tenantId, "POSTED", 1, 20);

        Assert.All(result, e => Assert.Equal("POSTED", e.Status));
    }

    [Fact]
    public async Task GetListAsync_ReturnsEmptyForNonExistentStatus()
    {
        await SeedTestEntries();
        var result = await _service.GetListAsync(_tenantId, "VOID", 1, 20);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetListAsync_RespectsPagination()
    {
        await SeedTestEntries();
        var result = await _service.GetListAsync(_tenantId, null, 1, 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetListAsync_OrdersByTransactionDateDescending()
    {
        await SeedTestEntries();
        var result = await _service.GetListAsync(_tenantId, null, 1, 20);

        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].TransactionDate >= result[i + 1].TransactionDate);
        }
    }

    [Fact]
    public async Task GetListAsync_ExcludesOtherTenants()
    {
        await SeedTestEntries();
        var otherTenantId = Guid.NewGuid();
        await _service.CreateAsync(otherTenantId,
            new CreateJournalEntryRequest(DateTime.UtcNow, "Other", CreateBalancedLines(), "DRAFT"),
            _userId);

        var result = await _service.GetListAsync(_tenantId, null, 1, 20);

        Assert.Equal(3, result.Count);
        Assert.All(result, e => Assert.Equal(_tenantId, e.TenantId));
    }

    // ─── GetByIdAsync Tests ─────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsEntryWithLines()
    {
        await SeedTestEntries();
        var existingEntry = await _db.JournalEntries.FirstAsync();

        var result = await _service.GetByIdAsync(existingEntry.Id, _tenantId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Lines);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForWrongTenant()
    {
        await SeedTestEntries();
        var existingEntry = await _db.JournalEntries.FirstAsync();

        var result = await _service.GetByIdAsync(existingEntry.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistentId()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), _tenantId);

        Assert.Null(result);
    }

    // ─── UpdateDraftAsync Tests ─────────────────────────────────────

    [Fact]
    public async Task UpdateDraftAsync_UpdatesEntryFields()
    {
        await SeedTestEntries();
        var draft = await _db.JournalEntries.FirstAsync(e => e.Status == "DRAFT");
        var request = new UpdateJournalEntryRequest(
            DateTime.UtcNow.AddDays(1),
            "Updated description",
            CreateBalancedLines(),
            "DRAFT"
        );

        var result = await _service.UpdateDraftAsync(draft.Id, _tenantId, request);

        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public async Task UpdateDraftAsync_ThrowsForPostedEntry()
    {
        await SeedTestEntries();
        var posted = await _db.JournalEntries.FirstAsync(e => e.Status == "POSTED");
        var request = new UpdateJournalEntryRequest(
            DateTime.UtcNow,
            "Try update",
            CreateBalancedLines(),
            "DRAFT"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateDraftAsync(posted.Id, _tenantId, request));
    }

    [Fact]
    public async Task UpdateDraftAsync_ThrowsForNonExistentEntry()
    {
        var request = new UpdateJournalEntryRequest(
            DateTime.UtcNow,
            "Not found",
            CreateBalancedLines(),
            "DRAFT"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateDraftAsync(Guid.NewGuid(), _tenantId, request));
    }

    [Fact]
    public async Task UpdateDraftAsync_AsSubmit_ValidatesBalance()
    {
        await SeedTestEntries();
        var draft = await _db.JournalEntries.FirstAsync(e => e.Status == "DRAFT");
        var request = new UpdateJournalEntryRequest(
            DateTime.UtcNow,
            "Unbalanced submit",
            CreateUnbalancedLines(),
            "SUBMIT"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateDraftAsync(draft.Id, _tenantId, request));
    }

    // ─── ApproveAsync Tests ─────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_ApprovesEntryAndSetsApprovedBy()
    {
        await SeedTestEntries();
        var pending = await _db.JournalEntries.FirstAsync(e => e.Status == "PENDING_APPROVAL");

        var result = await _service.ApproveAsync(pending.Id, _tenantId, _otherUserId);

        Assert.Equal("POSTED", result.Status);
        Assert.Equal(_otherUserId, result.ApprovedBy);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsForSelfApproval()
    {
        await SeedTestEntries();
        var pending = await _db.JournalEntries.FirstAsync(e => e.Status == "PENDING_APPROVAL");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(pending.Id, _tenantId, pending.CreatedBy));

        Assert.Equal("SELF_APPROVAL_DENIED", ex.Message);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsForNonPendingEntry()
    {
        await SeedTestEntries();
        var draft = await _db.JournalEntries.FirstAsync(e => e.Status == "DRAFT");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(draft.Id, _tenantId, _otherUserId));
    }

    [Fact]
    public async Task ApproveAsync_ThrowsForNonExistentEntry()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(Guid.NewGuid(), _tenantId, _otherUserId));
    }

    [Fact]
    public async Task ApproveAsync_ThrowsForWrongTenant()
    {
        await SeedTestEntries();
        var pending = await _db.JournalEntries.FirstAsync(e => e.Status == "PENDING_APPROVAL");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(pending.Id, Guid.NewGuid(), _otherUserId));
    }

    // ─── DeleteDraftAsync Tests ─────────────────────────────────────

    [Fact]
    public async Task DeleteDraftAsync_VoidsDraftEntry()
    {
        await SeedTestEntries();
        var draft = await _db.JournalEntries.FirstAsync(e => e.Status == "DRAFT");

        await _service.DeleteDraftAsync(draft.Id, _tenantId);

        var entry = await _db.JournalEntries.FindAsync(draft.Id);
        Assert.Equal("VOID", entry!.Status);
    }

    [Fact]
    public async Task DeleteDraftAsync_VoidsPendingEntry()
    {
        await SeedTestEntries();
        var pending = await _db.JournalEntries.FirstAsync(e => e.Status == "PENDING_APPROVAL");

        await _service.DeleteDraftAsync(pending.Id, _tenantId);

        var entry = await _db.JournalEntries.FindAsync(pending.Id);
        Assert.Equal("VOID", entry!.Status);
    }

    [Fact]
    public async Task DeleteDraftAsync_ThrowsForPostedEntry()
    {
        await SeedTestEntries();
        var posted = await _db.JournalEntries.FirstAsync(e => e.Status == "POSTED");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteDraftAsync(posted.Id, _tenantId));

        Assert.Equal("CANNOT_VOID_POSTED", ex.Message);
    }

    // ─── Helper Methods ────────────────────────────────────────────

    private async Task SeedTestEntries()
    {
        // Create 3 entries: DRAFT, POSTED, PENDING_APPROVAL
        await _service.CreateAsync(_tenantId,
            new CreateJournalEntryRequest(DateTime.UtcNow.AddDays(-1), "Draft Entry",
                CreateBalancedLines(1000000), "DRAFT"), _userId);

        await _service.CreateAsync(_tenantId,
            new CreateJournalEntryRequest(DateTime.UtcNow.AddDays(-2), "Posted Entry",
                CreateBalancedLines(10000000), "SUBMIT"), _userId);

        await _service.CreateAsync(_tenantId,
            new CreateJournalEntryRequest(DateTime.UtcNow, "Pending Entry",
                CreateBalancedLines(100000000), "SUBMIT"), _userId);
    }
}
