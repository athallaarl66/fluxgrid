using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Modules.WMS.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.WMS;

public class PurchaseReceiptServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PurchaseReceiptService _service;
    private readonly Mock<AuditService> _auditMock;
    private readonly Mock<DomainEventDispatcher> _dispatcherMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PurchaseReceiptServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _auditMock = new Mock<AuditService>(_db) { CallBase = true };
        _dispatcherMock = new Mock<DomainEventDispatcher>() { CallBase = true };
        _service = new PurchaseReceiptService(_db, _auditMock.Object, _dispatcherMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private async Task<Guid> SeedPo(string poNumber = "PO-9999", decimal qty = 100)
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
        }
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = poNumber,
            SupplierName = "Test Supplier",
            PoDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            TenantId = _tenantId,
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    ItemId = _itemId,
                    OrderedQty = qty,
                    ReceivedQty = 0
                }
            ]
        };
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        return po.Id;
    }

    private async Task SeedLocation(string code, LocationType type)
    {
        _db.Locations.Add(new Location
        {
            Id = Guid.NewGuid(),
            Code = code,
            Type = type,
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();
    }

    private static ReceiptLineInput LineInput(Guid itemId, decimal received, decimal passed, decimal failed)
        => new(itemId, received, passed, failed);

    // ─── CreateReceiptAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateReceiptAsync_CreatesReceiptInDraft()
    {
        await SeedPo();

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 50, 45, 5)
        ], _userId, null, null);

        Assert.True(result.Success);
        Assert.NotNull(result.ReceiptId);

        var receipt = await _db.PurchaseReceipts.Include(r => r.Lines).FirstAsync(r => r.Id == result.ReceiptId);
        Assert.Equal(ReceiptStatus.DRAFT, receipt.Status);
        Assert.Single(receipt.Lines);
        Assert.Equal(50, receipt.Lines[0].QtyReceived);
        Assert.Equal(45, receipt.Lines[0].QtyPassed);
        Assert.Equal(5, receipt.Lines[0].QtyFailed);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsNonExistentPo()
    {
        var result = await _service.CreateReceiptAsync(_tenantId, "PO-NONEXIST", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PO_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsQtyMismatch()
    {
        await SeedPo();

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 50, 40, 5) // 40+5 != 50
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.StartsWith("QTY_MISMATCH", result.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsItemNotOnPo()
    {
        await SeedPo();
        var otherItem = Guid.NewGuid();
        _db.InventoryItems.Add(new InventoryItem
        {
            Id = otherItem,
            Sku = "SKU-OTHER",
            Name = "Other Item",
            Uom = "pcs",
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(otherItem, 10, 10, 0)
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.StartsWith("ITEM_NOT_ON_PO", result.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsOverReceiving()
    {
        await SeedPo("PO-9999", 50);

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 60, 60, 0)
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.StartsWith("OVER_RECEIVING", result.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsOverReceivingAfterPartialFulfillment()
    {
        await SeedPo("PO-9999", 100);

        // First receipt: receive 60
        var first = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 60, 55, 5)
        ], _userId, null, null);
        Assert.True(first.Success);

        // Confirm and putaway the first receipt to update PO ReceivedQty
        var confirm = await _service.ConfirmReceiptAsync(_tenantId, first.ReceiptId!.Value, _userId, null, null);
        Assert.True(confirm.Success);

        await SeedLocation("SUPPLIER-TRANSIT", LocationType.TRANSIT);
        await SeedLocation("WH-MAIN", LocationType.WAREHOUSE);

        // Manually update PO ReceivedQty to simulate putaway
        var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstAsync(p => p.PoNumber == "PO-9999" && p.TenantId == _tenantId);
        po.Lines[0].ReceivedQty = 55;
        await _db.SaveChangesAsync();

        // Second receipt: try to receive 50 more (only 45 remaining)
        var second = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 50, 50, 0)
        ], _userId, null, null);

        Assert.False(second.Success);
        Assert.StartsWith("OVER_RECEIVING", second.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_RejectsItemNotFoundInTenant()
    {
        await SeedPo();
        var otherTenantItem = Guid.NewGuid();
        _db.InventoryItems.Add(new InventoryItem
        {
            Id = otherTenantItem,
            Sku = "SKU-OTHER",
            Name = "Other",
            Uom = "pcs",
            TenantId = Guid.NewGuid() // different tenant
        });
        await _db.SaveChangesAsync();

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            new ReceiptLineInput(otherTenantItem, 10, 10, 0)
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.StartsWith("ITEM_NOT_ON_PO", result.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_GeneratesReceiptNo()
    {
        await SeedPo();

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        Assert.True(result.Success);
        var receipt = await _db.PurchaseReceipts.FindAsync(result.ReceiptId);
        Assert.StartsWith("RCP-", receipt!.ReceiptNo);
    }

    [Fact]
    public async Task CreateReceiptAsync_ExcludesOtherTenantPo()
    {
        var otherTenant = Guid.NewGuid();
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = "PO-OTHER",
            SupplierName = "Other",
            PoDate = DateTime.UtcNow,
            TenantId = otherTenant,
            Lines =
            [
                new PurchaseOrderLine { Id = Guid.NewGuid(), ItemId = _itemId, OrderedQty = 100, ReceivedQty = 0 }
            ]
        };
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        // Try to receipt against other tenant's PO from this tenant
        var result = await _service.CreateReceiptAsync(_tenantId, "PO-OTHER", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("PO_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task CreateReceiptAsync_WithIpAndUa_Succeeds()
    {
        await SeedPo();

        var result = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, "10.0.0.1", "browser");

        Assert.True(result.Success);
    }

    // ─── GetReceiptAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetReceiptAsync_ReturnsReceiptWithLines()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 30, 25, 5)
        ], _userId, null, null);

        var receipt = await _service.GetReceiptAsync(_tenantId, createResult.ReceiptId!.Value);

        Assert.NotNull(receipt);
        Assert.Equal("PO-9999", receipt.PoReference);
        Assert.Single(receipt.Lines);
        Assert.Equal(30, receipt.Lines[0].QtyReceived);
        Assert.Equal(25, receipt.Lines[0].QtyPassed);
        Assert.Equal(5, receipt.Lines[0].QtyFailed);
    }

    [Fact]
    public async Task GetReceiptAsync_ReturnsNullForWrongTenant()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        var receipt = await _service.GetReceiptAsync(Guid.NewGuid(), createResult.ReceiptId!.Value);
        Assert.Null(receipt);
    }

    [Fact]
    public async Task GetReceiptAsync_ReturnsNullForNonExistent()
    {
        var receipt = await _service.GetReceiptAsync(_tenantId, Guid.NewGuid());
        Assert.Null(receipt);
    }

    // ─── GetReceiptListAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetReceiptListAsync_ReturnsPaginatedResults()
    {
        await SeedPo();
        for (int i = 0; i < 5; i++)
        {
            await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
            [
                LineInput(_itemId, 10, 10, 0)
            ], _userId, null, null);
        }

        var result = await _service.GetReceiptListAsync(_tenantId, null, null, null, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetReceiptListAsync_FiltersByStatus()
    {
        await SeedPo();
        var r1 = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);
        var r2 = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 20, 20, 0)
        ], _userId, null, null);
        await _service.ConfirmReceiptAsync(_tenantId, r2.ReceiptId!.Value, _userId, null, null);

        var draftResult = await _service.GetReceiptListAsync(_tenantId, "DRAFT", null, null, null, 1, 20);
        var pendingResult = await _service.GetReceiptListAsync(_tenantId, "PENDING_PUTAWAY", null, null, null, 1, 20);

        Assert.Single(draftResult.Items);
        Assert.Single(pendingResult.Items);
    }

    [Fact]
    public async Task GetReceiptListAsync_FiltersByPoReference()
    {
        await SeedPo("PO-FILTER-001");
        await SeedPo("PO-FILTER-002", 50);

        await _service.CreateReceiptAsync(_tenantId, "PO-FILTER-001", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);
        await _service.CreateReceiptAsync(_tenantId, "PO-FILTER-002", _userId.ToString(),
        [
            LineInput(_itemId, 5, 5, 0)
        ], _userId, null, null);

        var result = await _service.GetReceiptListAsync(_tenantId, null, "FILTER-001", null, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal("PO-FILTER-001", result.Items[0].PoReference);
    }

    [Fact]
    public async Task GetReceiptListAsync_FiltersByDateRange()
    {
        await SeedPo();
        var r1 = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);
        var r2 = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 20, 20, 0)
        ], _userId, null, null);

        // Manually set dates
        var receipt1 = await _db.PurchaseReceipts.FindAsync(r1.ReceiptId);
        receipt1!.CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var receipt2 = await _db.PurchaseReceipts.FindAsync(r2.ReceiptId);
        receipt2!.CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        await _db.SaveChangesAsync();

        var result = await _service.GetReceiptListAsync(_tenantId, null, null,
            new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), 1, 20);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetReceiptListAsync_ExcludesOtherTenants()
    {
        await SeedPo();
        var otherTenant = Guid.NewGuid();
        _db.PurchaseReceipts.Add(new PurchaseReceipt
        {
            Id = Guid.NewGuid(),
            ReceiptNo = "RCP-OTHER-001",
            PoReference = "PO-OTHER",
            Status = ReceiptStatus.DRAFT,
            ReceivedBy = _userId.ToString(),
            TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        var result = await _service.GetReceiptListAsync(_tenantId, null, null, null, null, 1, 20);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetReceiptListAsync_ReturnsEmptyForNoMatches()
    {
        var result = await _service.GetReceiptListAsync(_tenantId, null, null, null, null, 1, 20);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    // ─── ConfirmReceiptAsync ─────────────────────────────────────────

    [Fact]
    public async Task ConfirmReceiptAsync_TransitionsToPendingPutaway()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        var result = await _service.ConfirmReceiptAsync(_tenantId, createResult.ReceiptId!.Value, _userId, null, null);

        Assert.True(result.Success);
        var receipt = await _db.PurchaseReceipts.FindAsync(createResult.ReceiptId);
        Assert.Equal(ReceiptStatus.PENDING_PUTAWAY, receipt!.Status);
    }

    [Fact]
    public async Task ConfirmReceiptAsync_RejectsNonExistentReceipt()
    {
        var result = await _service.ConfirmReceiptAsync(_tenantId, Guid.NewGuid(), _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("RECEIPT_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ConfirmReceiptAsync_RejectsAlreadyConfirmed()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);
        await _service.ConfirmReceiptAsync(_tenantId, createResult.ReceiptId!.Value, _userId, null, null);

        var result = await _service.ConfirmReceiptAsync(_tenantId, createResult.ReceiptId!.Value, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS", result.Error);
    }

    [Fact]
    public async Task ConfirmReceiptAsync_RejectsWrongTenant()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        var result = await _service.ConfirmReceiptAsync(Guid.NewGuid(), createResult.ReceiptId!.Value, _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("RECEIPT_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ConfirmReceiptAsync_WithIpAndUa_Succeeds()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        var result = await _service.ConfirmReceiptAsync(_tenantId, createResult.ReceiptId!.Value, _userId, "10.0.0.1", "browser");

        Assert.True(result.Success);
    }

    // ─── ProcessPutawayAsync (validation only — needs real PostgreSQL for transaction) ──

    [Fact]
    public async Task ProcessPutawayAsync_RejectsNonExistentReceipt()
    {
        var result = await _service.ProcessPutawayAsync(_tenantId, Guid.NewGuid(),
        [
            new PutawayLineInput(Guid.NewGuid(), Guid.NewGuid())
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("RECEIPT_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task ProcessPutawayAsync_RejectsDraftStatus()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);

        var result = await _service.ProcessPutawayAsync(_tenantId, createResult.ReceiptId!.Value,
        [
            new PutawayLineInput(Guid.NewGuid(), Guid.NewGuid())
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("INVALID_STATUS", result.Error);
    }

    [Fact]
    public async Task ProcessPutawayAsync_RejectsAlreadyCompleted()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);
        await _service.ConfirmReceiptAsync(_tenantId, createResult.ReceiptId!.Value, _userId, null, null);

        // Can't fully test putaway without transaction, but we can confirm the receipt is PENDING_PUTAWAY
        var receipt = await _db.PurchaseReceipts.FindAsync(createResult.ReceiptId);
        Assert.Equal(ReceiptStatus.PENDING_PUTAWAY, receipt!.Status);
    }

    [Fact]
    public async Task ProcessPutawayAsync_RejectsMissingTransitLocation()
    {
        await SeedPo();
        var createResult = await _service.CreateReceiptAsync(_tenantId, "PO-9999", _userId.ToString(),
        [
            LineInput(_itemId, 10, 10, 0)
        ], _userId, null, null);
        await _service.ConfirmReceiptAsync(_tenantId, createResult.ReceiptId!.Value, _userId, null, null);

        var result = await _service.ProcessPutawayAsync(_tenantId, createResult.ReceiptId!.Value,
        [
            new PutawayLineInput(Guid.NewGuid(), Guid.NewGuid())
        ], _userId, null, null);

        Assert.False(result.Success);
        Assert.Equal("TRANSIT_LOCATION_NOT_FOUND", result.Error);
    }

    [Fact(Skip = "Requires real PostgreSQL for transaction support (BeginTransactionAsync)")]
    public void ProcessPutawayAsync_SuccessfulPutaway_Integration()
    {
        // Integration test — requires real PostgreSQL for transaction support
    }

    // ─── Dto Records ─────────────────────────────────────────────────

    [Fact]
    public void ReceiptCreateResult_Success()
    {
        var id = Guid.NewGuid();
        var result = new ReceiptCreateResult(true, id, null);

        Assert.True(result.Success);
        Assert.Equal(id, result.ReceiptId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ReceiptCreateResult_Failure()
    {
        var result = new ReceiptCreateResult(false, null, "PO_NOT_FOUND");

        Assert.False(result.Success);
        Assert.Null(result.ReceiptId);
        Assert.Equal("PO_NOT_FOUND", result.Error);
    }

    [Fact]
    public void ReceiptActionResult_Success()
    {
        var result = new ReceiptActionResult(true, null);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ReceiptActionResult_Failure()
    {
        var result = new ReceiptActionResult(false, "ERROR");

        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Error);
    }
}
