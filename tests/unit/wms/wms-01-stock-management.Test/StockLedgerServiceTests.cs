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

public class StockLedgerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly StockLedgerService _service;
    private readonly Mock<AuditService> _auditMock;
    private readonly Mock<DomainEventDispatcher> _dispatcherMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();

    public StockLedgerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _auditMock = new Mock<AuditService>(_db) { CallBase = true };
        _dispatcherMock = new Mock<DomainEventDispatcher>() { CallBase = true };
        _cacheMock = new Mock<ICacheService>();
        _service = new StockLedgerService(_db, _auditMock.Object, _dispatcherMock.Object, _cacheMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── GetLedgerAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetLedgerAsync_ReturnsPaginatedResults()
    {
        SeedLedgerEntries(5).ForEach(e => _db.StockLedgerEntries.Add(e));
        await _db.SaveChangesAsync();

        var result = await _service.GetLedgerAsync(_tenantId, null, null, null, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetLedgerAsync_FiltersBySku()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        SeedInventoryItems(itemA, "SKU-A", itemB, "SKU-B");
        SeedLedgerEntries(3, itemA).ForEach(e => _db.StockLedgerEntries.Add(e));
        SeedLedgerEntries(2, itemB).ForEach(e => _db.StockLedgerEntries.Add(e));
        await _db.SaveChangesAsync();

        var result = await _service.GetLedgerAsync(_tenantId, "SKU-A", null, null, null, 1, 20);

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetLedgerAsync_FiltersByDateRange()
    {
        var entries = SeedLedgerEntries(5);
        entries[0].CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        entries[1].CreatedAt = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        entries[2].CreatedAt = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);
        entries.ForEach(e => _db.StockLedgerEntries.Add(e));
        await _db.SaveChangesAsync();

        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = await _service.GetLedgerAsync(_tenantId, null, null, start, end, 1, 20);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetLedgerAsync_FiltersByLocationId()
    {
        var locA = Guid.NewGuid();
        var locB = Guid.NewGuid();
        SeedLedgerEntries(3, _itemId, locA).ForEach(e => _db.StockLedgerEntries.Add(e));
        SeedLedgerEntries(2, _itemId, locB).ForEach(e => _db.StockLedgerEntries.Add(e));
        await _db.SaveChangesAsync();

        var result = await _service.GetLedgerAsync(_tenantId, null, locA, null, null, 1, 20);

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetLedgerAsync_ExcludesOtherTenants()
    {
        SeedLedgerEntries(3).ForEach(e =>
        {
            e.TenantId = _tenantId;
            _db.StockLedgerEntries.Add(e);
        });
        SeedLedgerEntries(2).ForEach(e =>
        {
            e.TenantId = Guid.NewGuid();
            _db.StockLedgerEntries.Add(e);
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetLedgerAsync(_tenantId, null, null, null, null, 1, 20);

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetLedgerAsync_ReturnsEmptyForNoMatches()
    {
        var result = await _service.GetLedgerAsync(_tenantId, "NONEXISTENT", null, null, null, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetLedgerAsync_OrdersByCreatedAtDescending()
    {
        var entries = SeedLedgerEntries(3);
        entries[0].CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        entries[1].CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        entries[2].CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        entries.ForEach(e => _db.StockLedgerEntries.Add(e));
        await _db.SaveChangesAsync();

        var result = await _service.GetLedgerAsync(_tenantId, null, null, null, null, 1, 20);

        for (int i = 0; i < result.Items.Count - 1; i++)
            Assert.True(result.Items[i].CreatedAt >= result.Items[i + 1].CreatedAt);
    }

    // ─── GetBalanceAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetBalanceAsync_ReturnsBalance()
    {
        _db.InventoryBalances.Add(new InventoryBalance
        {
            Id = Guid.NewGuid(),
            ItemId = _itemId,
            LocationId = _locationId,
            BalanceQty = 100,
            BalanceValue = 1000000,
            TenantId = _tenantId,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetBalanceAsync(_tenantId, _itemId, _locationId);

        Assert.NotNull(result);
        Assert.Equal(100, result.BalanceQty);
        Assert.Equal(1000000, result.BalanceValue);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsNullForNonExistent()
    {
        var result = await _service.GetBalanceAsync(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBalanceAsync_ExcludesOtherTenants()
    {
        _db.InventoryBalances.Add(new InventoryBalance
        {
            Id = Guid.NewGuid(),
            ItemId = _itemId,
            LocationId = _locationId,
            BalanceQty = 100,
            BalanceValue = 1000000,
            TenantId = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetBalanceAsync(_tenantId, _itemId, _locationId);
        Assert.Null(result);
    }

    // ─── CalculateFifoValuationAsync ─────────────────────────────────

    [Fact]
    public async Task CalculateFifoValuationAsync_SingleLayer_ReturnsCorrectAverage()
    {
        await SeedPositiveEntries([(10, 1000)]);

        var result = await _service.CalculateFifoValuationAsync(_tenantId, _itemId);

        Assert.Single(result.Layers);
        Assert.Equal(10, result.TotalQuantity);
        Assert.Equal(10000, result.TotalValue);
        Assert.Equal(1000, result.AverageCost);
    }

    [Fact]
    public async Task CalculateFifoValuationAsync_MultipleLayers_ReturnsAllLayers()
    {
        await SeedPositiveEntries([(10, 1000), (5, 1500)]);

        var result = await _service.CalculateFifoValuationAsync(_tenantId, _itemId);

        Assert.Equal(2, result.Layers.Count);
        Assert.Equal(15, result.TotalQuantity);
        Assert.Equal(17500, result.TotalValue);
    }

    [Fact]
    public async Task CalculateFifoValuationAsync_ExcludesNegativeEntries()
    {
        await SeedPositiveEntries([(10, 1000)]);
        _db.StockLedgerEntries.Add(CreateEntry(_itemId, _tenantId, -5, 1000, DateTime.UtcNow));
        await _db.SaveChangesAsync();

        var result = await _service.CalculateFifoValuationAsync(_tenantId, _itemId);

        Assert.Single(result.Layers);
        Assert.Equal(10, result.TotalQuantity);
    }

    [Fact]
    public async Task CalculateFifoValuationAsync_ReturnsZeroForNoEntries()
    {
        var result = await _service.CalculateFifoValuationAsync(_tenantId, _itemId);

        Assert.Empty(result.Layers);
        Assert.Equal(0, result.TotalQuantity);
        Assert.Equal(0, result.TotalValue);
        Assert.Equal(0, result.AverageCost);
    }

    [Fact]
    public async Task CalculateFifoValuationAsync_OrdersByCreatedAtAscending()
    {
        var laterId = Guid.NewGuid();
        var earlierId = Guid.NewGuid();
        _db.StockLedgerEntries.Add(CreateEntry(_itemId, _tenantId, 5, 2000, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), laterId));
        _db.StockLedgerEntries.Add(CreateEntry(_itemId, _tenantId, 10, 1000, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), earlierId));
        await _db.SaveChangesAsync();

        var result = await _service.CalculateFifoValuationAsync(_tenantId, _itemId);

        Assert.Equal(earlierId, result.Layers[0].EntryId);
        Assert.Equal(laterId, result.Layers[1].EntryId);
    }

    // ─── CalculateAverageCostValuationAsync ──────────────────────────

    [Fact]
    public async Task CalculateAverageCostValuationAsync_ReturnsWeightedAverage()
    {
        await SeedPositiveEntries([(10, 1000), (10, 1500)]);

        var result = await _service.CalculateAverageCostValuationAsync(_tenantId, _itemId);

        Assert.Equal(1250, result.AverageCost); // (10*1000 + 10*1500) / 20
        Assert.Equal(20, result.TotalQuantity);
        Assert.Equal(25000, result.TotalValue);
    }

    [Fact]
    public async Task CalculateAverageCostValuationAsync_SingleEntry()
    {
        await SeedPositiveEntries([(10, 500)]);

        var result = await _service.CalculateAverageCostValuationAsync(_tenantId, _itemId);

        Assert.Equal(500, result.AverageCost);
    }

    [Fact]
    public async Task CalculateAverageCostValuationAsync_ReturnsZeroForNoEntries()
    {
        var result = await _service.CalculateAverageCostValuationAsync(_tenantId, _itemId);

        Assert.Equal(0, result.AverageCost);
        Assert.Equal(0, result.TotalQuantity);
        Assert.Equal(0, result.TotalValue);
    }

    [Fact]
    public async Task CalculateAverageCostValuationAsync_ExcludesNegativeEntries()
    {
        await SeedPositiveEntries([(10, 1000)]);
        _db.StockLedgerEntries.Add(CreateEntry(_itemId, _tenantId, -5, 1000, DateTime.UtcNow));
        await _db.SaveChangesAsync();

        var result = await _service.CalculateAverageCostValuationAsync(_tenantId, _itemId);

        Assert.Equal(10, result.TotalQuantity);
    }

    [Fact]
    public async Task CalculateAverageCostValuationAsync_ExcludesOtherTenants()
    {
        await SeedPositiveEntries([(10, 1000)]);
        _db.StockLedgerEntries.Add(CreateEntry(_itemId, Guid.NewGuid(), 10, 9999, DateTime.UtcNow));
        await _db.SaveChangesAsync();

        var result = await _service.CalculateAverageCostValuationAsync(_tenantId, _itemId);

        Assert.Equal(1000, result.AverageCost);
    }

    // ─── CreateMovementAsync Validation (pre-transaction) ────────────

    [Fact]
    public async Task CreateMovementAsync_RejectsSingleEntry()
    {
        var entries = new List<CreateMovementEntry>
        {
            new(_itemId, _locationId, 100, 1000, "PURCHASE_RECEIPT", Guid.NewGuid())
        };

        var result = await _service.CreateMovementAsync(_tenantId, entries, Guid.NewGuid(), null, null);

        Assert.False(result.Success);
        Assert.Contains("At least two entries", result.Error);
    }

    [Fact]
    public async Task CreateMovementAsync_RejectsUnbalancedEntries()
    {
        var entries = new List<CreateMovementEntry>
        {
            new(_itemId, _locationId, 100, 1000, "PURCHASE_RECEIPT", Guid.NewGuid()),
            new(_itemId, Guid.NewGuid(), -50, 1000, "PURCHASE_RECEIPT", Guid.NewGuid())
        };

        var result = await _service.CreateMovementAsync(_tenantId, entries, Guid.NewGuid(), null, null);

        Assert.False(result.Success);
        Assert.Contains("must equal zero", result.Error);
    }

    [Fact]
    public async Task CreateMovementAsync_RejectsZeroEntries()
    {
        var result = await _service.CreateMovementAsync(_tenantId, [], Guid.NewGuid(), null, null);

        Assert.False(result.Success);
        Assert.Contains("At least two entries", result.Error);
    }

    [Fact(Skip = "Requires real PostgreSQL for transaction support (BeginTransactionAsync)")]
    public void CreateMovementAsync_AcceptsBalancedEntries_Integration()
    {
        // Integration test — requires real PostgreSQL for transaction + FOR UPDATE support
    }

    // ─── Dto Types ──────────────────────────────────────────────────

    [Fact]
    public void CreateMovementEntry_PropertiesMatchConstructor()
    {
        var entry = new CreateMovementEntry(_itemId, _locationId, 100, 1000, "PURCHASE_RECEIPT", Guid.NewGuid());

        Assert.Equal(_itemId, entry.ItemId);
        Assert.Equal(_locationId, entry.LocationId);
        Assert.Equal(100, entry.Quantity);
        Assert.Equal(1000, entry.UnitCost);
        Assert.Equal("PURCHASE_RECEIPT", entry.ReferenceType);
    }

    [Fact]
    public void LedgerResult_PropertiesMatchConstructor()
    {
        var items = new List<StockLedgerEntry> { new() };
        var result = new LedgerResult(items, 1, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public void MovementResult_Success()
    {
        var id = Guid.NewGuid();
        var result = new MovementResult(true, id, null);

        Assert.True(result.Success);
        Assert.Equal(id, result.TransactionId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void MovementResult_Failure()
    {
        var result = new MovementResult(false, null, "Error occurred");

        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.Equal("Error occurred", result.Error);
    }

    [Fact]
    public void FifoValuationResult_Properties()
    {
        var layers = new List<CostLayer> { new(Guid.NewGuid(), 10, 1000, DateTime.UtcNow) };
        var result = new FifoValuationResult(layers, 10, 10000, 1000);

        Assert.Single(result.Layers);
        Assert.Equal(10, result.TotalQuantity);
        Assert.Equal(10000, result.TotalValue);
        Assert.Equal(1000, result.AverageCost);
    }

    [Fact]
    public void AverageCostResult_Properties()
    {
        var result = new AverageCostResult(1250, 20, 25000);

        Assert.Equal(1250, result.AverageCost);
        Assert.Equal(20, result.TotalQuantity);
        Assert.Equal(25000, result.TotalValue);
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private List<StockLedgerEntry> SeedLedgerEntries(int count, Guid? itemId = null, Guid? locationId = null)
    {
        var list = new List<StockLedgerEntry>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = Guid.NewGuid(),
                ItemId = itemId ?? _itemId,
                LocationId = locationId ?? _locationId,
                Quantity = 100 * (i + 1),
                UnitCost = 1000,
                ReferenceType = "PURCHASE_RECEIPT",
                ReferenceId = Guid.NewGuid(),
                TenantId = _tenantId,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        return list;
    }

    private void SeedInventoryItems(Guid idA, string skuA, Guid idB, string skuB)
    {
        _db.InventoryItems.AddRange(
            new InventoryItem { Id = idA, Sku = skuA, Name = "Item A", Uom = "pcs", TenantId = _tenantId },
            new InventoryItem { Id = idB, Sku = skuB, Name = "Item B", Uom = "pcs", TenantId = _tenantId }
        );
    }

    private async Task SeedPositiveEntries(params (int qty, decimal cost)[] layers)
    {
        foreach (var (qty, cost) in layers)
        {
            _db.StockLedgerEntries.Add(CreateEntry(_itemId, _tenantId, qty, cost, DateTime.UtcNow));
        }
        await _db.SaveChangesAsync();
    }

    private static StockLedgerEntry CreateEntry(Guid itemId, Guid tenantId, decimal qty, decimal cost, DateTime createdAt, Guid? id = null)
    {
        return new StockLedgerEntry
        {
            Id = id ?? Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            ItemId = itemId,
            LocationId = Guid.NewGuid(),
            Quantity = qty,
            UnitCost = cost,
            ReferenceType = "PURCHASE_RECEIPT",
            ReferenceId = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = createdAt
        };
    }
}
