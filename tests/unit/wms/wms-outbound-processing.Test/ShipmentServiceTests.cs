using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Caching;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.WMS;

public class ShipmentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShipmentService _service;
    private readonly Mock<AuditService> _auditMock;
    private readonly Mock<DomainEventDispatcher> _dispatcherMock;
    private readonly StockLedgerService _ledgerService;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ShipmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _auditMock = new Mock<AuditService>(_db) { CallBase = true };
        _dispatcherMock = new Mock<DomainEventDispatcher>() { CallBase = true };
        _cacheMock = new Mock<ICacheService>();
        _ledgerService = new StockLedgerService(_db, _auditMock.Object, _dispatcherMock.Object, _cacheMock.Object);
        _service = new ShipmentService(_db, _auditMock.Object, _dispatcherMock.Object, _ledgerService);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── Helpers ─────────────────────────────────────────────────────

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

    private async Task<Guid> SeedPackedOrder(string orderNo = "SO-PACKED")
    {
        await SeedItem();
        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = orderNo,
            Status = SalesOrderStatus.PACKED,
            CustomerId = _customerId,
            CustomerName = "Test Customer",
            TenantId = _tenantId,
            Lines =
            [
                new SalesOrderLine
                {
                    Id = Guid.NewGuid(),
                    ItemId = _itemId,
                    QtyOrdered = 100,
                    QtyReserved = 100,
                    QtyPicked = 100,
                    QtyShipped = 0
                }
            ]
        };
        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();
        return order.Id;
    }

    // ─── VerifyPackingAsync ──────────────────────────────────────────

    [Fact]
    public async Task VerifyPackingAsync_VerifiesMatchingQtys()
    {
        var orderId = await SeedPackedOrder();
        var orderLine = await _db.SalesOrderLines.FirstAsync(l => l.ItemId == _itemId);

        var result = await _service.VerifyPackingAsync(_tenantId, orderId,
            [new VerifyLineInput(_itemId, 100)], _userId, null, null);

        Assert.True(result.Success);
        Assert.Null(result.Error);

        var order = await _db.SalesOrders.FindAsync(orderId);
        Assert.Equal(SalesOrderStatus.PACKED, order!.Status);
    }

    [Fact]
    public async Task VerifyPackingAsync_RejectsNonExistentOrder()
    {
        var result = await _service.VerifyPackingAsync(_tenantId, Guid.NewGuid(),
            [new VerifyLineInput(_itemId, 10)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task VerifyPackingAsync_RejectsWrongTenant()
    {
        var orderId = await SeedPackedOrder();

        var result = await _service.VerifyPackingAsync(Guid.NewGuid(), orderId,
            [new VerifyLineInput(_itemId, 100)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task VerifyPackingAsync_RejectsItemNotOnOrder()
    {
        var orderId = await SeedPackedOrder();
        var otherItem = Guid.NewGuid();

        var result = await _service.VerifyPackingAsync(_tenantId, orderId,
            [new VerifyLineInput(otherItem, 100)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PACKING_MISMATCH", result.Error);
        Assert.Contains("ITEM_NOT_ON_ORDER", result.ErrorDetail!);
    }

    [Fact]
    public async Task VerifyPackingAsync_RejectsQtyMismatch()
    {
        var orderId = await SeedPackedOrder();

        var result = await _service.VerifyPackingAsync(_tenantId, orderId,
            [new VerifyLineInput(_itemId, 50)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PACKING_MISMATCH", result.Error);
        Assert.Contains("PACKING_MISMATCH", result.ErrorDetail!);
    }

    [Fact]
    public async Task VerifyPackingAsync_WithIpAndUa_Succeeds()
    {
        var orderId = await SeedPackedOrder();

        var result = await _service.VerifyPackingAsync(_tenantId, orderId,
            [new VerifyLineInput(_itemId, 100)], _userId, "10.0.0.1", "browser");

        Assert.True(result.Success);
    }

    // ─── ConfirmShipmentAsync (validation only — needs real PostgreSQL for ledger tx) ──

    [Fact]
    public async Task ConfirmShipmentAsync_RejectsNonExistentOrder()
    {
        var result = await _service.ConfirmShipmentAsync(_tenantId, Guid.NewGuid(), _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ConfirmShipmentAsync_RejectsWrongTenant()
    {
        var orderId = await SeedPackedOrder();

        var result = await _service.ConfirmShipmentAsync(Guid.NewGuid(), orderId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ConfirmShipmentAsync_RejectsNonPackedStatus()
    {
        await SeedItem();
        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-PICKING",
            Status = SalesOrderStatus.PICKING,
            CustomerId = _customerId,
            CustomerName = "Test",
            TenantId = _tenantId
        };
        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _service.ConfirmShipmentAsync(_tenantId, order.Id, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS_TRANSITION", result.Error);
    }

    [Fact]
    public async Task ConfirmShipmentAsync_RejectsAlreadyConfirmed()
    {
        var orderId = await SeedPackedOrder();

        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNo = "SHP-001",
            OrderId = orderId,
            Status = "SHIPPED",
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.ConfirmShipmentAsync(_tenantId, orderId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("SHIPMENT_ALREADY_CONFIRMED", result.Error);
    }

    [Fact(Skip = "Requires real PostgreSQL for transaction support (BeginTransactionAsync)")]
    public void ConfirmShipmentAsync_SuccessfulShipment_Integration()
    {
        // Integration test — requires real PostgreSQL for transaction support
    }

    // ─── GetShipmentListAsync ────────────────────────────────────────

    [Fact]
    public async Task GetShipmentListAsync_ReturnsPaginatedResults()
    {
        var now = DateTime.UtcNow;
        var orderIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        foreach (var oid in orderIds)
        {
            _db.SalesOrders.Add(new SalesOrder { Id = oid, OrderNo = $"SO-{oid}", Status = SalesOrderStatus.SHIPPED, CustomerId = _customerId, CustomerName = "C", TenantId = _tenantId });
        }
        for (int i = 0; i < 5; i++)
        {
            _db.Shipments.Add(new Shipment
            {
                Id = Guid.NewGuid(),
                ShipmentNo = $"SHP-{i:D4}",
                OrderId = orderIds[i],
                Status = "SHIPPED",
                ShippedAt = now.AddMinutes(-i),
                TenantId = _tenantId
            });
        }
        await _db.SaveChangesAsync();

        var result = await _service.GetShipmentListAsync(_tenantId, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetShipmentListAsync_FiltersByOrderId()
    {
        var targetOrderId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _db.SalesOrders.Add(new SalesOrder { Id = targetOrderId, OrderNo = "SO-TARGET", Status = SalesOrderStatus.SHIPPED, CustomerId = _customerId, CustomerName = "C", TenantId = _tenantId });
        var otherOrderId = Guid.NewGuid();
        _db.SalesOrders.Add(new SalesOrder { Id = otherOrderId, OrderNo = "SO-OTHER", Status = SalesOrderStatus.SHIPPED, CustomerId = _customerId, CustomerName = "C", TenantId = _tenantId });
        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNo = "SHP-TARGET",
            OrderId = targetOrderId,
            Status = "SHIPPED",
            ShippedAt = now,
            TenantId = _tenantId
        });
        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNo = "SHP-OTHER",
            OrderId = otherOrderId,
            Status = "SHIPPED",
            ShippedAt = now,
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetShipmentListAsync(_tenantId, targetOrderId, 1, 20);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetShipmentListAsync_ExcludesOtherTenants()
    {
        var now = DateTime.UtcNow;
        var mineOrderId = Guid.NewGuid();
        var theirOrderId = Guid.NewGuid();
        _db.SalesOrders.Add(new SalesOrder { Id = mineOrderId, OrderNo = "SO-MINE", Status = SalesOrderStatus.SHIPPED, CustomerId = _customerId, CustomerName = "C", TenantId = _tenantId });
        _db.SalesOrders.Add(new SalesOrder { Id = theirOrderId, OrderNo = "SO-THEIRS", Status = SalesOrderStatus.SHIPPED, CustomerId = _customerId, CustomerName = "C", TenantId = Guid.NewGuid() });
        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNo = "SHP-MINE",
            OrderId = mineOrderId,
            Status = "SHIPPED",
            ShippedAt = now,
            TenantId = _tenantId
        });
        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNo = "SHP-THEIRS",
            OrderId = theirOrderId,
            Status = "SHIPPED",
            ShippedAt = now,
            TenantId = Guid.NewGuid()
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetShipmentListAsync(_tenantId, null, 1, 20);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetShipmentListAsync_ReturnsEmptyForNoMatches()
    {
        var result = await _service.GetShipmentListAsync(_tenantId, null, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    // ─── Dto Records ─────────────────────────────────────────────────

    [Fact]
    public void VerifyResult_Success()
    {
        var result = new VerifyResult(true, null, null);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void VerifyResult_Failure()
    {
        var result = new VerifyResult(false, "detail", "PACKING_MISMATCH");

        Assert.False(result.Success);
        Assert.Equal("detail", result.ErrorDetail);
        Assert.Equal("PACKING_MISMATCH", result.Error);
    }

    [Fact]
    public void ShipConfirmResult_Success()
    {
        var id = Guid.NewGuid();
        var result = new ShipConfirmResult(true, id, null);

        Assert.True(result.Success);
        Assert.Equal(id, result.ShipmentId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ShipConfirmResult_Failure()
    {
        var result = new ShipConfirmResult(false, null, "ORDER_NOT_FOUND");

        Assert.False(result.Success);
        Assert.Null(result.ShipmentId);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }
}
