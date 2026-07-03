using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Caching;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.Finance;

public class ChartOfAccountServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ChartOfAccountService _service;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ChartOfAccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var audit = new AuditService(_db);
        var events = new DomainEventDispatcher();
        _cacheMock = new Mock<ICacheService>();

        _cacheMock
            .Setup(c => c.GetAsync<List<AccountTreeNode>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<AccountTreeNode>?)null);

        _service = new ChartOfAccountService(_db, audit, events, _cacheMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedAccounts()
    {
        var assets = new ChartOfAccount
        {
            Id = Guid.NewGuid(), Code = "1000", Name = "Assets",
            Type = "ASSET", IsActive = true, TenantId = _tenantId
        };
        var currentAssets = new ChartOfAccount
        {
            Id = Guid.NewGuid(), Code = "1100", Name = "Current Assets",
            Type = "ASSET", IsActive = true, TenantId = _tenantId, ParentId = assets.Id
        };
        var cash = new ChartOfAccount
        {
            Id = Guid.NewGuid(), Code = "1110", Name = "Cash in Bank",
            Type = "ASSET", IsActive = true, TenantId = _tenantId, ParentId = currentAssets.Id
        };
        _db.ChartOfAccounts.AddRange(assets, currentAssets, cash);
        await _db.SaveChangesAsync();
    }

    // ─── GetTreeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetTreeAsync_ReturnsTree_Hierarchical()
    {
        await SeedAccounts();

        var tree = await _service.GetTreeAsync(_tenantId);

        Assert.Single(tree);
        Assert.Equal("1000", tree[0].Code);
        Assert.Single(tree[0].Children);
        Assert.Equal("1100", tree[0].Children[0].Code);
        Assert.Single(tree[0].Children[0].Children);
        Assert.Equal("1110", tree[0].Children[0].Children[0].Code);
    }

    [Fact]
    public async Task GetTreeAsync_ReturnsFlatList_WhenFlatIsTrue()
    {
        await SeedAccounts();

        var flat = await _service.GetTreeAsync(_tenantId, flat: true);

        Assert.Equal(3, flat.Count);
        Assert.True(flat.All(n => n.Children.Count == 0));
    }

    [Fact]
    public async Task GetTreeAsync_ReturnsEmpty_WhenNoAccounts()
    {
        var tree = await _service.GetTreeAsync(_tenantId);
        Assert.Empty(tree);
    }

    [Fact]
    public async Task GetTreeAsync_UsesCache()
    {
        await SeedAccounts();
        await _service.GetTreeAsync(_tenantId);

        _cacheMock.Verify(c => c.GetAsync<List<AccountTreeNode>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<AccountTreeNode>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTreeAsync_ReturnsCached_WhenAvailable()
    {
        var cached = new List<AccountTreeNode> { new(Guid.NewGuid(), "9999", "Cached", null, "ASSET", true, 0, []) };
        _cacheMock
            .Setup(c => c.GetAsync<List<AccountTreeNode>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _service.GetTreeAsync(_tenantId);

        Assert.Single(result);
        Assert.Equal("Cached", result[0].Name);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<AccountTreeNode>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── CreateAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesTopLevelAccount()
    {
        var request = new CreateAccountRequest("6000", "Income", null, "REVENUE");

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.Equal("6000", result.Code);
        Assert.Equal("Income", result.Name);
        Assert.Equal("REVENUE", result.Type);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenTypeInvalid()
    {
        var request = new CreateAccountRequest("6000", "Test", null, "INVALID");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCodeDuplicate()
    {
        await SeedAccounts();
        var request = new CreateAccountRequest("1000", "Duplicate", null, "ASSET");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InheritsTypeFromParent()
    {
        await SeedAccounts();
        var assets = await _db.ChartOfAccounts.FirstAsync(a => a.Code == "1000");
        var request = new CreateAccountRequest("1300", "Other Assets", assets.Id, "ASSET");

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.Equal("ASSET", result.Type);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenParentNotFound()
    {
        var request = new CreateAccountRequest("1300", "Orphan", Guid.NewGuid(), "ASSET");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
    }

    // ─── UpdateAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesAccountName()
    {
        await SeedAccounts();
        var account = await _db.ChartOfAccounts.FirstAsync(a => a.Code == "1110");
        var request = new UpdateAccountRequest(null, "Updated Name", null, null, null);

        var result = await _service.UpdateAsync(account.Id, _tenantId, request, Guid.NewGuid());

        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenAccountNotFound()
    {
        var request = new UpdateAccountRequest("9999", null, null, null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(_tenantId, Guid.NewGuid(), request, Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenCodeDuplicate()
    {
        await SeedAccounts();
        var target = await _db.ChartOfAccounts.FirstAsync(a => a.Code == "1110");
        var request = new UpdateAccountRequest("1100", null, null, null, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(target.Id, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("already exists", ex.Message);
    }

    // ─── DeactivateAsync ────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_DeactivatesAccountAndChildren()
    {
        await SeedAccounts();
        var assets = await _db.ChartOfAccounts.FirstAsync(a => a.Code == "1000");

        await _service.DeactivateAsync(assets.Id, _tenantId, Guid.NewGuid());

        var all = await _db.ChartOfAccounts.Where(a => a.TenantId == _tenantId).ToListAsync();
        Assert.True(all.All(a => !a.IsActive));
    }

    [Fact]
    public async Task DeactivateAsync_Throws_WhenAlreadyInactive()
    {
        await SeedAccounts();
        var account = await _db.ChartOfAccounts.FirstAsync(a => a.Code == "1110");
        account.IsActive = false;
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeactivateAsync(_tenantId, account.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeactivateAsync_Throws_WhenAccountNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeactivateAsync(_tenantId, Guid.NewGuid(), Guid.NewGuid()));
    }
}
