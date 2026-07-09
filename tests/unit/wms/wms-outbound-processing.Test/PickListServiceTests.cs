using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.WMS;

public class PickListServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PickListService _service;
    private readonly Mock<AuditService> _auditMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PickListServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _auditMock = new Mock<AuditService>(_db) { CallBase = true };
        _service = new PickListService(_db, _auditMock.Object);
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

    private async Task<Guid> SeedOrder(string orderNo = "SO-9999", decimal qty = 100)
    {
        await SeedItem();
        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = orderNo,
            Status = SalesOrderStatus.PENDING,
            CustomerId = _customerId,
            CustomerName = "Test Customer",
            TenantId = _tenantId,
            Lines =
            [
                new SalesOrderLine
                {
                    Id = Guid.NewGuid(),
                    ItemId = _itemId,
                    QtyOrdered = qty,
                    QtyReserved = 0,
                    QtyPicked = 0,
                    QtyShipped = 0
                }
            ]
        };
        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();
        return order.Id;
    }

    private async Task<(Guid pickListId, Guid pickItemId, Guid orderId)> SeedPickList(PickListStatus status = PickListStatus.GENERATED)
    {
        var orderId = await SeedOrder();
        var order = await _db.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId);
        var orderLine = order.Lines[0];
        var pickItemId = Guid.NewGuid();
        var pickListId = Guid.NewGuid();

        _db.PickLists.Add(new PickList
        {
            Id = pickListId,
            OrderId = orderId,
            Status = status,
            TenantId = _tenantId,
            Order = order,
            Items =
            [
                new PickListItem
                {
                    Id = pickItemId,
                    OrderLineId = orderLine.Id,
                    ItemId = _itemId,
                    LocationId = Guid.NewGuid(),
                    QtyExpected = 100,
                    QtyPicked = 0
                }
            ]
        });
        await _db.SaveChangesAsync();
        return (pickListId, pickItemId, orderId);
    }

    // ─── GeneratePickListAsync ───────────────────────────────────────

    [Fact]
    public async Task GeneratePickListAsync_RejectsNonExistentOrder()
    {
        var result = await _service.GeneratePickListAsync(_tenantId, Guid.NewGuid(), _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task GeneratePickListAsync_RejectsOtherTenantOrder()
    {
        var otherTenant = Guid.NewGuid();
        _db.SalesOrders.Add(new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-OTHER",
            Status = SalesOrderStatus.PENDING,
            CustomerId = _customerId,
            CustomerName = "Other",
            TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        var otherOrderId = (await _db.SalesOrders.FirstAsync(o => o.TenantId == otherTenant)).Id;
        var result = await _service.GeneratePickListAsync(_tenantId, otherOrderId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task GeneratePickListAsync_RejectsDuplicatePickList()
    {
        var orderId = await SeedOrder();

        _db.PickLists.Add(new PickList
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = PickListStatus.GENERATED,
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GeneratePickListAsync(_tenantId, orderId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("DUPLICATE_PICK_LIST", result.Error);
    }

    [Fact(Skip = "Requires real PostgreSQL for transaction support (BeginTransactionAsync)")]
    public void GeneratePickListAsync_AllowsNewPickListAfterCancelled_Integration()
    {
    }

    [Fact(Skip = "Requires real PostgreSQL for transaction support (BeginTransactionAsync)")]
    public void GeneratePickListAsync_SuccessfulGeneration_Integration()
    {
    }

    [Fact(Skip = "Requires real PostgreSQL for transaction support (BeginTransactionAsync)")]
    public void GeneratePickListAsync_RejectsInsufficientStock_Integration()
    {
    }

    // ─── GetPickListAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetPickListAsync_ReturnsPickListWithItems()
    {
        var (pickListId, _, orderId) = await SeedPickList();

        var pickList = await _service.GetPickListAsync(_tenantId, pickListId);

        Assert.NotNull(pickList);
        Assert.Equal(orderId, pickList.OrderId);
        Assert.Single(pickList.Items);
        Assert.Equal(100, pickList.Items[0].QtyExpected);
    }

    [Fact]
    public async Task GetPickListAsync_ReturnsNullForWrongTenant()
    {
        var (pickListId, _, _) = await SeedPickList();

        var result = await _service.GetPickListAsync(Guid.NewGuid(), pickListId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPickListAsync_ReturnsNullForNonExistent()
    {
        var result = await _service.GetPickListAsync(_tenantId, Guid.NewGuid());
        Assert.Null(result);
    }

    // ─── ExecutePickItemsAsync ───────────────────────────────────────

    [Fact]
    public async Task ExecutePickItemsAsync_CompletesFullPick()
    {
        var (pickListId, pickItemId, orderId) = await SeedPickList();

        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(pickItemId, 100, null)], _userId, null, null);

        Assert.True(result.Success);
        var pl = await _db.PickLists.Include(p => p.Items).FirstAsync(p => p.Id == pickListId);
        Assert.Equal(PickListStatus.COMPLETED, pl.Status);
        Assert.Equal(100, pl.Items[0].QtyPicked);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_ShortPickWithReason_Succeeds()
    {
        var (pickListId, pickItemId, _) = await SeedPickList();

        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(pickItemId, 80, "Damaged")], _userId, null, null);

        Assert.True(result.Success);
        var pl = await _db.PickLists.Include(p => p.Items).FirstAsync(p => p.Id == pickListId);
        Assert.Equal(PickListStatus.IN_PROGRESS, pl.Status);
        Assert.Equal(80, pl.Items[0].QtyPicked);
        Assert.Equal("Damaged", pl.Items[0].ShortPickReason);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_RejectsPickListNotFound()
    {
        var result = await _service.ExecutePickItemsAsync(_tenantId, Guid.NewGuid(),
            [new PickExecutionInput(Guid.NewGuid(), 10, null)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PICK_LIST_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_RejectsCompletedStatus()
    {
        var (pickListId, _, _) = await SeedPickList(PickListStatus.COMPLETED);

        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(Guid.NewGuid(), 10, null)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS", result.Error);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_RejectsCancelledStatus()
    {
        var (pickListId, _, _) = await SeedPickList(PickListStatus.CANCELLED);

        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(Guid.NewGuid(), 10, null)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS", result.Error);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_RejectsItemNotFound()
    {
        var orderId = await SeedOrder();
        _db.PickLists.Add(new PickList
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = PickListStatus.GENERATED,
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var pickListId = (await _db.PickLists.FirstAsync(p => p.TenantId == _tenantId)).Id;
        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(Guid.NewGuid(), 10, null)], _userId, null, null);

        Assert.False(result.Success);
        Assert.StartsWith("ITEM_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_RejectsShortPickWithoutReason()
    {
        var (pickListId, pickItemId, _) = await SeedPickList();

        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(pickItemId, 50, null)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("SHORT_PICK_REASON_REQUIRED", result.Error);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_RejectsWrongTenant()
    {
        var (pickListId, _, _) = await SeedPickList();

        var result = await _service.ExecutePickItemsAsync(Guid.NewGuid(), pickListId,
            [new PickExecutionInput(Guid.NewGuid(), 10, null)], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PICK_LIST_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ExecutePickItemsAsync_WithIpAndUa_Succeeds()
    {
        var (pickListId, pickItemId, _) = await SeedPickList();

        var result = await _service.ExecutePickItemsAsync(_tenantId, pickListId,
            [new PickExecutionInput(pickItemId, 100, null)], _userId, "10.0.0.1", "browser");

        Assert.True(result.Success);
    }

    // ─── CancelPickListAsync ─────────────────────────────────────────

    [Fact]
    public async Task CancelPickListAsync_CancelsAndUnreservesStock()
    {
        var (pickListId, _, orderId) = await SeedPickList();
        var orderLine = await _db.SalesOrderLines.FirstAsync(l => l.ItemId == _itemId);
        orderLine.QtyReserved = 100;
        await _db.SaveChangesAsync();

        var result = await _service.CancelPickListAsync(_tenantId, pickListId, _userId, null, null);

        Assert.True(result.Success);
        var pl = await _db.PickLists.FindAsync(pickListId);
        Assert.Equal(PickListStatus.CANCELLED, pl!.Status);

        var order = await _db.SalesOrders.Include(o => o.Lines).FirstAsync(o => o.Id == orderId);
        Assert.Equal(SalesOrderStatus.PENDING, order.Status);
        Assert.Equal(0, order.Lines[0].QtyReserved);
    }

    [Fact]
    public async Task CancelPickListAsync_RejectsNonExistent()
    {
        var result = await _service.CancelPickListAsync(_tenantId, Guid.NewGuid(), _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PICK_LIST_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task CancelPickListAsync_RejectsCompleted()
    {
        var (pickListId, _, _) = await SeedPickList(PickListStatus.COMPLETED);

        var result = await _service.CancelPickListAsync(_tenantId, pickListId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS", result.Error);
    }

    [Fact]
    public async Task CancelPickListAsync_RejectsCancelled()
    {
        var (pickListId, _, _) = await SeedPickList(PickListStatus.CANCELLED);

        var result = await _service.CancelPickListAsync(_tenantId, pickListId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS", result.Error);
    }

    [Fact]
    public async Task CancelPickListAsync_RejectsWrongTenant()
    {
        var (pickListId, _, _) = await SeedPickList();

        var result = await _service.CancelPickListAsync(Guid.NewGuid(), pickListId, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PICK_LIST_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task CancelPickListAsync_WithIpAndUa_Succeeds()
    {
        var (pickListId, _, _) = await SeedPickList();

        var result = await _service.CancelPickListAsync(_tenantId, pickListId, _userId, "10.0.0.1", "browser");

        Assert.True(result.Success);
    }

    // ─── Dto Records ─────────────────────────────────────────────────

    [Fact]
    public void PickListCreateResult_Success()
    {
        var id = Guid.NewGuid();
        var result = new PickListCreateResult(true, id, null);

        Assert.True(result.Success);
        Assert.Equal(id, result.PickListId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void PickListCreateResult_Failure()
    {
        var result = new PickListCreateResult(false, null, "ORDER_NOT_FOUND");

        Assert.False(result.Success);
        Assert.Null(result.PickListId);
        Assert.Equal("ORDER_NOT_FOUND", result.Error);
    }

    [Fact]
    public void PickListActionResult_Success()
    {
        var result = new PickListActionResult(true, null);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void PickListActionResult_Failure()
    {
        var result = new PickListActionResult(false, "PICK_LIST_NOT_FOUND");

        Assert.False(result.Success);
        Assert.Equal("PICK_LIST_NOT_FOUND", result.Error);
    }
}
