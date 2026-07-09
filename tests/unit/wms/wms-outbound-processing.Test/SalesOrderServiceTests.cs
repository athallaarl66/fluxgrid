using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.WMS;

public class SalesOrderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SalesOrderService _service;
    private readonly Mock<AuditService> _auditMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SalesOrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _auditMock = new Mock<AuditService>(_db) { CallBase = true };
        _service = new SalesOrderService(_db, _auditMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedItem()
    {
        if (!await _db.InventoryItems.AnyAsync(i => i.Id == _itemId))
        {
            _db.InventoryItems.Add(new InventoryItem
            {
                Id = _itemId,
                Sku = "SKU-TEST",
                Name = "Test Item",
                Uom = "pcs",
                TenantId = _tenantId
            });
            await _db.SaveChangesAsync();
        }
    }

    // ─── CreateOrderAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateOrderAsync_CreatesOrderWithLines()
    {
        await SeedItem();

        var result = await _service.CreateOrderAsync(_tenantId, "SO-001", _customerId, "Customer A", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);

        var order = await _db.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == result.OrderId);
        Assert.Equal("SO-001", order.OrderNo);
        Assert.Equal(SalesOrderStatus.PENDING, order.Status);
        Assert.Equal("Customer A", order.CustomerName);
        Assert.Single(order.Lines);
        Assert.Equal(10, order.Lines[0].QtyOrdered);
        Assert.Equal(0, order.Lines[0].QtyReserved);
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsDuplicateOrderNo()
    {
        await SeedItem();
        await _service.CreateOrderAsync(_tenantId, "SO-001", _customerId, "A", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var result = await _service.CreateOrderAsync(_tenantId, "SO-001", _customerId, "B", null,
            [new SoLineInput(_itemId, 20)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("DUPLICATE_ORDER_NO", result.Error);
    }

    [Fact]
    public async Task CreateOrderAsync_AllowsSameOrderNoAcrossTenants()
    {
        await SeedItem();
        await _service.CreateOrderAsync(_tenantId, "SO-001", _customerId, "A", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var result = await _service.CreateOrderAsync(Guid.NewGuid(), "SO-001", _customerId, "B", null,
            [new SoLineInput(_itemId, 20)], _userId, null, null);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateOrderAsync_CreatesMultipleLines()
    {
        await SeedItem();
        var itemB = Guid.NewGuid();
        _db.InventoryItems.Add(new InventoryItem { Id = itemB, Sku = "SKU-B", Name = "Item B", Uom = "pcs", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var result = await _service.CreateOrderAsync(_tenantId, "SO-002", _customerId, "A", null,
            [new SoLineInput(_itemId, 50), new SoLineInput(itemB, 30)], _userId, null, null);

        Assert.True(result.Success);
        var order = await _db.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == result.OrderId);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNotes_Succeeds()
    {
        await SeedItem();

        var result = await _service.CreateOrderAsync(_tenantId, "SO-NOTES", _customerId, "Customer", "Urgent",
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        Assert.True(result.Success);
        var order = await _db.SalesOrders.FindAsync(result.OrderId);
        Assert.Equal("Urgent", order!.Notes);
    }

    [Fact]
    public async Task CreateOrderAsync_WithIpAndUa_Succeeds()
    {
        await SeedItem();

        var result = await _service.CreateOrderAsync(_tenantId, "SO-IPUA", _customerId, "Customer", null,
            [new SoLineInput(_itemId, 10)], _userId, "127.0.0.1", "test-agent");

        Assert.True(result.Success);
    }

    // ─── GetOrderAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetOrderAsync_ReturnsOrderWithLines()
    {
        await SeedItem();
        var createResult = await _service.CreateOrderAsync(_tenantId, "SO-003", _customerId, "Customer A", null,
            [new SoLineInput(_itemId, 100)], _userId, null, null);

        var order = await _service.GetOrderAsync(_tenantId, createResult.OrderId!.Value);

        Assert.NotNull(order);
        Assert.Equal("SO-003", order.OrderNo);
        Assert.Single(order.Lines);
        Assert.Equal(100, order.Lines[0].QtyOrdered);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsNullForWrongTenant()
    {
        await SeedItem();
        var createResult = await _service.CreateOrderAsync(_tenantId, "SO-004", _customerId, "Customer", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var order = await _service.GetOrderAsync(Guid.NewGuid(), createResult.OrderId!.Value);
        Assert.Null(order);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsNullForNonExistent()
    {
        var order = await _service.GetOrderAsync(_tenantId, Guid.NewGuid());
        Assert.Null(order);
    }

    // ─── GetOrderListAsync ────────────────────────────────────────

    [Fact]
    public async Task GetOrderListAsync_ReturnsPaginatedResults()
    {
        await SeedItem();
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreateOrderAsync(_tenantId, $"SO-{i:D3}", _customerId, "Customer", null,
                [new SoLineInput(_itemId, 10)], _userId, null, null);
        }

        var result = await _service.GetOrderListAsync(_tenantId, null, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetOrderListAsync_SearchesByOrderNo()
    {
        await SeedItem();
        await _service.CreateOrderAsync(_tenantId, "SO-SEARCH-001", _customerId, "A", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);
        await _service.CreateOrderAsync(_tenantId, "SO-OTHER-002", _customerId, "B", null,
            [new SoLineInput(_itemId, 20)], _userId, null, null);

        var result = await _service.GetOrderListAsync(_tenantId, "SEARCH", null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("SO-SEARCH-001", result.Items[0].OrderNo);
    }

    [Fact]
    public async Task GetOrderListAsync_SearchesByCustomerName()
    {
        await SeedItem();
        await _service.CreateOrderAsync(_tenantId, "SO-010", _customerId, "Acme Corp", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);
        await _service.CreateOrderAsync(_tenantId, "SO-011", _customerId, "Beta Inc", null,
            [new SoLineInput(_itemId, 20)], _userId, null, null);

        var result = await _service.GetOrderListAsync(_tenantId, "Acme", null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("Acme Corp", result.Items[0].CustomerName);
    }

    [Fact]
    public async Task GetOrderListAsync_FiltersByStatus()
    {
        await SeedItem();
        var r1 = await _service.CreateOrderAsync(_tenantId, "SO-020", _customerId, "A", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        _ = await _service.CancelOrderAsync(_tenantId, r1.OrderId!.Value, _userId, null, null);

        var result = await _service.GetOrderListAsync(_tenantId, null, "CANCELLED", 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("CANCELLED", result.Items[0].Status);
    }

    [Fact]
    public async Task GetOrderListAsync_ExcludesOtherTenants()
    {
        await SeedItem();
        await _service.CreateOrderAsync(_tenantId, "SO-MINE", _customerId, "Mine", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);
        await _service.CreateOrderAsync(Guid.NewGuid(), "SO-THEIRS", _customerId, "Theirs", null,
            [new SoLineInput(_itemId, 20)], _userId, null, null);

        var result = await _service.GetOrderListAsync(_tenantId, null, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("SO-MINE", result.Items[0].OrderNo);
    }

    [Fact]
    public async Task GetOrderListAsync_ReturnsEmptyForNoMatches()
    {
        var result = await _service.GetOrderListAsync(_tenantId, null, null, 1, 20);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetOrderListAsync_OrdersByCreatedAtDescending()
    {
        await SeedItem();
        var dates = new[]
        {
            new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        foreach (var date in dates)
        {
            var result = await _service.CreateOrderAsync(_tenantId, $"SO-{date:yyyyMMdd}", _customerId, "Customer", null,
                [new SoLineInput(_itemId, 10)], _userId, null, null);
            var order = await _db.SalesOrders.FindAsync(result.OrderId);
            order!.CreatedAt = date;
        }
        await _db.SaveChangesAsync();

        var list = await _service.GetOrderListAsync(_tenantId, null, null, 1, 20);

        for (int i = 0; i < list.Items.Count - 1; i++)
            Assert.True(list.Items[i].CreatedAt >= list.Items[i + 1].CreatedAt);
    }

    // ─── CancelOrderAsync ─────────────────────────────────────────

    [Fact]
    public async Task CancelOrderAsync_CancelsOrderAndUnreservesStock()
    {
        await SeedItem();
        var createResult = await _service.CreateOrderAsync(_tenantId, "SO-CANCEL", _customerId, "Customer", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var result = await _service.CancelOrderAsync(_tenantId, createResult.OrderId!.Value, _userId, null, null);

        Assert.True(result.Success);
        var order = await _db.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == createResult.OrderId);
        Assert.Equal(SalesOrderStatus.CANCELLED, order.Status);
        Assert.Equal(0, order.Lines[0].QtyReserved);
    }

    [Fact]
    public async Task CancelOrderAsync_RejectsNonExistentOrder()
    {
        var result = await _service.CancelOrderAsync(_tenantId, Guid.NewGuid(), _userId, null, null);
        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task CancelOrderAsync_RejectsShippedOrder()
    {
        await SeedItem();
        var createResult = await _service.CreateOrderAsync(_tenantId, "SO-SHIPPED", _customerId, "Customer", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var order = await _db.SalesOrders.FindAsync(createResult.OrderId);
        order!.Status = SalesOrderStatus.SHIPPED;
        await _db.SaveChangesAsync();

        var result = await _service.CancelOrderAsync(_tenantId, createResult.OrderId!.Value, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS_TRANSITION", result.Error);
    }

    [Fact]
    public async Task CancelOrderAsync_RejectsWrongTenant()
    {
        await SeedItem();
        var createResult = await _service.CreateOrderAsync(_tenantId, "SO-WRONG", _customerId, "Customer", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var result = await _service.CancelOrderAsync(Guid.NewGuid(), createResult.OrderId!.Value, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task CancelOrderAsync_WithIpAndUa_Succeeds()
    {
        await SeedItem();
        var createResult = await _service.CreateOrderAsync(_tenantId, "SO-IPUA", _customerId, "Customer", null,
            [new SoLineInput(_itemId, 10)], _userId, null, null);

        var result = await _service.CancelOrderAsync(_tenantId, createResult.OrderId!.Value, _userId, "10.0.0.1", "browser");

        Assert.True(result.Success);
    }

    // ─── Dto Records ──────────────────────────────────────────────

    [Fact]
    public void SoCreateResult_Success()
    {
        var id = Guid.NewGuid();
        var result = new SoCreateResult(true, id, null);

        Assert.True(result.Success);
        Assert.Equal(id, result.OrderId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void SoCreateResult_Failure()
    {
        var result = new SoCreateResult(false, null, "ERROR");

        Assert.False(result.Success);
        Assert.Null(result.OrderId);
        Assert.Equal("ERROR", result.Error);
    }

    [Fact]
    public void SoActionResult_Success()
    {
        var result = new SoActionResult(true, null);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void SoActionResult_Failure()
    {
        var result = new SoActionResult(false, "ORDER_NOT_FOUND");

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }
}
