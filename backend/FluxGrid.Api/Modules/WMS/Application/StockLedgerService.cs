using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Caching;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.Application;

public class StockLedgerService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly ICacheService _cache;

    public StockLedgerService(AppDbContext db, AuditService audit, DomainEventDispatcher dispatcher, ICacheService cache)
    {
        _db = db;
        _audit = audit;
        _dispatcher = dispatcher;
        _cache = cache;
    }

    public async Task<LedgerResult> GetLedgerAsync(Guid tenantId, string? sku, Guid? locationId, string? locationCode, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var query = _db.StockLedgerEntries
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrEmpty(sku))
        {
            var itemIds = await _db.InventoryItems
                .Where(i => i.TenantId == tenantId && i.Sku == sku)
                .Select(i => i.Id)
                .ToListAsync();
            query = query.Where(e => itemIds.Contains(e.ItemId));
        }

        if (locationId.HasValue)
            query = query.Where(e => e.LocationId == locationId.Value);

        if (!string.IsNullOrEmpty(locationCode))
        {
            var locIds = await _db.Locations
                .Where(l => l.TenantId == tenantId && l.Code == locationCode)
                .Select(l => l.Id)
                .ToListAsync();
            if (locIds.Count > 0)
                query = query.Where(e => locIds.Contains(e.LocationId));
        }

        if (startDate.HasValue)
            query = query.Where(e => e.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.CreatedAt <= endDate.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new LedgerResult(items, total, page, pageSize);
    }

    public async Task<MovementResult> CreateMovementAsync(Guid tenantId, List<CreateMovementEntry> entries, Guid userId, string? ipAddress, string? userAgent)
    {
        if (entries.Count < 2)
            return new MovementResult(false, null, "At least two entries (debit + credit) are required");

        if (Math.Abs(entries.Sum(e => e.Quantity)) > 0.001m)
            return new MovementResult(false, null, "Sum of quantities must equal zero (double-entry)");

        var transactionId = Guid.NewGuid();
        var ledgerEntries = new List<StockLedgerEntry>();
        var now = DateTime.UtcNow;

        foreach (var e in entries)
        {
            ledgerEntries.Add(new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                ItemId = e.ItemId,
                LocationId = e.LocationId,
                Quantity = e.Quantity,
                UnitCost = e.UnitCost,
                ReferenceType = e.ReferenceType,
                ReferenceId = e.ReferenceId,
                TenantId = tenantId,
                CreatedAt = now
            });
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var entry in ledgerEntries.Where(e => e.Quantity < 0))
            {
                var balance = await _db.InventoryBalances
                    .FromSqlRaw("SELECT * FROM inventory_balances WHERE item_id = {0} AND location_id = {1} AND tenant_id = {2} FOR UPDATE",
                        entry.ItemId, entry.LocationId, tenantId)
                    .FirstOrDefaultAsync();

                if (balance == null || balance.BalanceQty + entry.Quantity < 0)
                    return new MovementResult(false, null, "Insufficient stock");
            }

            _db.StockLedgerEntries.AddRange(ledgerEntries);

            foreach (var entry in ledgerEntries)
            {
                var balance = await _db.InventoryBalances
                    .FirstOrDefaultAsync(b => b.ItemId == entry.ItemId && b.LocationId == entry.LocationId && b.TenantId == tenantId);

                if (balance == null)
                {
                    balance = new InventoryBalance
                    {
                        Id = Guid.NewGuid(),
                        ItemId = entry.ItemId,
                        LocationId = entry.LocationId,
                        BalanceQty = entry.Quantity > 0 ? entry.Quantity : 0,
                        BalanceValue = entry.Quantity > 0 ? entry.Quantity * entry.UnitCost : 0,
                        TenantId = tenantId,
                        UpdatedAt = now
                    };
                    _db.InventoryBalances.Add(balance);
                }
                else
                {
                    balance.BalanceQty += entry.Quantity;
                    balance.BalanceValue += entry.Quantity * entry.UnitCost;
                    balance.UpdatedAt = now;
                }
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(userId, tenantId, "CREATE", "stock_ledger", transactionId, ipAddress, userAgent);

            foreach (var entry in ledgerEntries)
            {
                _dispatcher.Raise(new StockMovement
                {
                    ItemId = entry.ItemId,
                    LocationId = entry.LocationId,
                    Quantity = entry.Quantity,
                    UnitCost = entry.UnitCost,
                    ReferenceType = entry.ReferenceType,
                    OccurredOn = now
                });
            }

            await tx.CommitAsync();

            return new MovementResult(true, transactionId, null);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<InventoryBalance?> GetBalanceAsync(Guid tenantId, Guid itemId, Guid locationId)
    {
        return await _db.InventoryBalances
            .FirstOrDefaultAsync(b => b.ItemId == itemId && b.LocationId == locationId && b.TenantId == tenantId);
    }

    public async Task<FifoValuationResult> CalculateFifoValuationAsync(Guid tenantId, Guid itemId)
    {
        var entries = await _db.StockLedgerEntries
            .Where(e => e.TenantId == tenantId && e.ItemId == itemId && e.Quantity > 0)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var layers = entries.Select(e => new CostLayer(e.Id, e.Quantity, e.UnitCost, e.CreatedAt)).ToList();
        var totalQty = layers.Sum(l => l.Quantity);
        var totalValue = layers.Sum(l => l.Quantity * l.UnitCost);
        var averageCost = totalQty > 0 ? totalValue / totalQty : 0;

        return new FifoValuationResult(layers, totalQty, totalValue, averageCost);
    }

    public async Task<AverageCostResult> CalculateAverageCostValuationAsync(Guid tenantId, Guid itemId)
    {
        var entries = await _db.StockLedgerEntries
            .Where(e => e.TenantId == tenantId && e.ItemId == itemId && e.Quantity > 0)
            .ToListAsync();

        var totalQty = entries.Sum(e => e.Quantity);
        var totalValue = entries.Sum(e => e.Quantity * e.UnitCost);
        var averageCost = totalQty > 0 ? totalValue / totalQty : 0;

        return new AverageCostResult(averageCost, totalQty, totalValue);
    }
}

public record LedgerResult(List<StockLedgerEntry> Items, int Total, int Page, int PageSize);
public record MovementResult(bool Success, Guid? TransactionId, string? Error);
public record CreateMovementEntry(Guid ItemId, Guid LocationId, decimal Quantity, decimal UnitCost, string ReferenceType, Guid ReferenceId);
public record CostLayer(Guid EntryId, decimal Quantity, decimal UnitCost, DateTime CreatedAt);
public record FifoValuationResult(List<CostLayer> Layers, decimal TotalQuantity, decimal TotalValue, decimal AverageCost);
public record AverageCostResult(decimal AverageCost, decimal TotalQuantity, decimal TotalValue);
