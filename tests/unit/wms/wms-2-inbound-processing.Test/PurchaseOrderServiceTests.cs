using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.WMS;

public class PurchaseOrderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PurchaseOrderService _service;
    private readonly Mock<AuditService> _auditMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();

    public PurchaseOrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _auditMock = new Mock<AuditService>(_db) { CallBase = true };
        _service = new PurchaseOrderService(_db, _auditMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── CreatePoAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreatePoAsync_CreatesPoWithLines()
    {
        var result = await _service.CreatePoAsync(_tenantId, "PO-001", "Supplier A", new DateTime(2026, 7, 1),
        [
            new PoLineInput(_itemId, 100)
        ], Guid.NewGuid(), null, null);

        Assert.True(result.Success);
        Assert.NotNull(result.PoId);

        var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstAsync(p => p.Id == result.PoId);
        Assert.Equal("PO-001", po.PoNumber);
        Assert.Equal("Supplier A", po.SupplierName);
        Assert.Single(po.Lines);
        Assert.Equal(100, po.Lines[0].OrderedQty);
        Assert.Equal(0, po.Lines[0].ReceivedQty);
    }

    [Fact]
    public async Task CreatePoAsync_RejectsDuplicatePoNumber()
    {
        await _service.CreatePoAsync(_tenantId, "PO-001", "Supplier A", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), null, null);

        var result = await _service.CreatePoAsync(_tenantId, "PO-001", "Supplier B", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 20)
        ], Guid.NewGuid(), null, null);

        Assert.False(result.Success);
        Assert.Equal("PO_NUMBER_EXISTS", result.Error);
    }

    [Fact]
    public async Task CreatePoAsync_AllowsSamePoNumberAcrossTenants()
    {
        var otherTenant = Guid.NewGuid();
        await _service.CreatePoAsync(_tenantId, "PO-001", "Supplier A", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), null, null);

        var result = await _service.CreatePoAsync(otherTenant, "PO-001", "Supplier B", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 20)
        ], Guid.NewGuid(), null, null);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreatePoAsync_CreatesMultipleLines()
    {
        var itemB = Guid.NewGuid();
        var result = await _service.CreatePoAsync(_tenantId, "PO-002", "Supplier A", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 50),
            new PoLineInput(itemB, 30)
        ], Guid.NewGuid(), null, null);

        Assert.True(result.Success);
        var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstAsync(p => p.Id == result.PoId);
        Assert.Equal(2, po.Lines.Count);
    }

    [Fact]
    public async Task CreatePoAsync_WithIpAndUa_Succeeds()
    {
        var result = await _service.CreatePoAsync(_tenantId, "PO-AUDIT", "Supplier", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), "127.0.0.1", "test-agent");

        Assert.True(result.Success);
    }

    // ─── GetPoByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetPoByIdAsync_ReturnsPoWithLines()
    {
        var createResult = await _service.CreatePoAsync(_tenantId, "PO-003", "Supplier A", new DateTime(2026, 7, 1),
        [
            new PoLineInput(_itemId, 100)
        ], Guid.NewGuid(), null, null);

        var po = await _service.GetPoByIdAsync(_tenantId, createResult.PoId!.Value);

        Assert.NotNull(po);
        Assert.Equal("PO-003", po.PoNumber);
        Assert.Single(po.Lines);
        Assert.Equal(100, po.Lines[0].OrderedQty);
    }

    [Fact]
    public async Task GetPoByIdAsync_ReturnsNullForWrongTenant()
    {
        var createResult = await _service.CreatePoAsync(_tenantId, "PO-004", "Supplier", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), null, null);

        var po = await _service.GetPoByIdAsync(Guid.NewGuid(), createResult.PoId!.Value);
        Assert.Null(po);
    }

    [Fact]
    public async Task GetPoByIdAsync_ReturnsNullForNonExistent()
    {
        var po = await _service.GetPoByIdAsync(_tenantId, Guid.NewGuid());
        Assert.Null(po);
    }

    // ─── GetPoListAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetPoListAsync_ReturnsPaginatedResults()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreatePoAsync(_tenantId, $"PO-{i:D3}", "Supplier", DateTime.UtcNow,
            [
                new PoLineInput(_itemId, 10)
            ], Guid.NewGuid(), null, null);
        }

        var result = await _service.GetPoListAsync(_tenantId, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetPoListAsync_SearchesByPoNumber()
    {
        await _service.CreatePoAsync(_tenantId, "PO-SEARCH-001", "Supplier A", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), null, null);
        await _service.CreatePoAsync(_tenantId, "PO-OTHER-002", "Supplier B", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 20)
        ], Guid.NewGuid(), null, null);

        var result = await _service.GetPoListAsync(_tenantId, "SEARCH", 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("PO-SEARCH-001", result.Items[0].PoNumber);
    }

    [Fact]
    public async Task GetPoListAsync_SearchesBySupplierName()
    {
        await _service.CreatePoAsync(_tenantId, "PO-010", "Acme Corp", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), null, null);
        await _service.CreatePoAsync(_tenantId, "PO-011", "Beta Inc", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 20)
        ], Guid.NewGuid(), null, null);

        var result = await _service.GetPoListAsync(_tenantId, "Acme", 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("Acme Corp", result.Items[0].SupplierName);
    }

    [Fact]
    public async Task GetPoListAsync_ExcludesOtherTenants()
    {
        await _service.CreatePoAsync(_tenantId, "PO-MINE", "Mine", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 10)
        ], Guid.NewGuid(), null, null);
        await _service.CreatePoAsync(Guid.NewGuid(), "PO-THEIRS", "Theirs", DateTime.UtcNow,
        [
            new PoLineInput(_itemId, 20)
        ], Guid.NewGuid(), null, null);

        var result = await _service.GetPoListAsync(_tenantId, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("PO-MINE", result.Items[0].PoNumber);
    }

    [Fact]
    public async Task GetPoListAsync_ReturnsEmptyForNoMatches()
    {
        var result = await _service.GetPoListAsync(_tenantId, null, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetPoListAsync_OrdersByPoDateDescending()
    {
        var dates = new[]
        {
            new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        foreach (var date in dates)
        {
            await _service.CreatePoAsync(_tenantId, $"PO-{date:yyyyMMdd}", "Supplier", date,
            [
                new PoLineInput(_itemId, 10)
            ], Guid.NewGuid(), null, null);
        }

        var result = await _service.GetPoListAsync(_tenantId, null, 1, 20);

        for (int i = 0; i < result.Items.Count - 1; i++)
            Assert.True(result.Items[i].PoDate >= result.Items[i + 1].PoDate);
    }

    // ─── Dto Records ─────────────────────────────────────────────────

    [Fact]
    public void PoCreateResult_Success()
    {
        var id = Guid.NewGuid();
        var result = new PoCreateResult(true, id, null);

        Assert.True(result.Success);
        Assert.Equal(id, result.PoId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void PoCreateResult_Failure()
    {
        var result = new PoCreateResult(false, null, "ERROR");

        Assert.False(result.Success);
        Assert.Null(result.PoId);
        Assert.Equal("ERROR", result.Error);
    }
}
