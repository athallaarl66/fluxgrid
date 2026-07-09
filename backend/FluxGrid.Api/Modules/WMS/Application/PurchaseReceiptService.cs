using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Modules.WMS.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.Application;

public class PurchaseReceiptService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _dispatcher;

    public PurchaseReceiptService(AppDbContext db, AuditService audit, DomainEventDispatcher dispatcher)
    {
        _db = db;
        _audit = audit;
        _dispatcher = dispatcher;
    }

    public async Task<ReceiptCreateResult> CreateReceiptAsync(Guid tenantId, string poReference, string receivedBy, List<ReceiptLineInput> lines, Guid userId, string? ip, string? ua)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PoNumber == poReference);

        if (po is null)
            return new ReceiptCreateResult(false, null, "PO_NOT_FOUND");

        foreach (var line in lines)
        {
            if (line.QtyPassed + line.QtyFailed != line.QtyReceived)
                return new ReceiptCreateResult(false, null, $"QTY_MISMATCH: line item {line.ItemId} — passed + failed != received");

            var poLine = po.Lines.FirstOrDefault(pl => pl.ItemId == line.ItemId);
            if (poLine is null)
                return new ReceiptCreateResult(false, null, $"ITEM_NOT_ON_PO: {line.ItemId}");

            var remaining = poLine.OrderedQty - poLine.ReceivedQty;
            if (line.QtyReceived > remaining)
                return new ReceiptCreateResult(false, null, $"OVER_RECEIVING: item {line.ItemId} — ordered {poLine.OrderedQty}, already received {poLine.ReceivedQty}, cannot receive {line.QtyReceived}");

            if (line.QtyPassed > 0 || line.QtyFailed > 0)
            {
                var itemExists = await _db.InventoryItems.AnyAsync(i => i.Id == line.ItemId && i.TenantId == tenantId);
                if (!itemExists)
                    return new ReceiptCreateResult(false, null, $"ITEM_NOT_FOUND: {line.ItemId}");
            }
        }

        var nextNumber = await _db.PurchaseReceipts.CountAsync() + 1;
        var receiptNo = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{nextNumber:D4}";

        var receipt = new PurchaseReceipt
        {
            Id = Guid.NewGuid(),
            ReceiptNo = receiptNo,
            PoReference = poReference,
            Status = ReceiptStatus.DRAFT,
            ReceivedBy = receivedBy,
            TenantId = tenantId,
            Lines = lines.Select(l => new PurchaseReceiptLine
            {
                Id = Guid.NewGuid(),
                ItemId = l.ItemId,
                OrderedQty = po.Lines.First(pl => pl.ItemId == l.ItemId).OrderedQty,
                QtyReceived = l.QtyReceived,
                QtyPassed = l.QtyPassed,
                QtyFailed = l.QtyFailed
            }).ToList()
        };

        _db.PurchaseReceipts.Add(receipt);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "CREATE", "purchase_receipt", receipt.Id, ip, ua);

        return new ReceiptCreateResult(true, receipt.Id, null);
    }

    public async Task<ReceiptDetail?> GetReceiptAsync(Guid tenantId, Guid id)
    {
        var receipt = await _db.PurchaseReceipts
            .Include(r => r.Lines)
            .ThenInclude(l => l.Item)
            .Include(r => r.Lines)
            .ThenInclude(l => l.PutawayLoc)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (receipt is null) return null;
        return MapDetail(receipt);
    }

    public async Task<ReceiptListResult> GetReceiptListAsync(Guid tenantId, string? status, string? poReference, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        var query = _db.PurchaseReceipts.Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReceiptStatus>(status, true, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);

        if (!string.IsNullOrEmpty(poReference))
            query = query.Where(r => r.PoReference.Contains(poReference));

        if (startDate.HasValue)
            query = query.Where(r => r.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.CreatedAt <= endDate.Value);

        var total = await query.CountAsync();
        var items = await query
            .Include(r => r.Lines)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ReceiptListResult(items.Select(MapDetail).ToList(), total, page, pageSize);
    }

    public async Task<ReceiptActionResult> ConfirmReceiptAsync(Guid tenantId, Guid receiptId, Guid userId, string? ip, string? ua)
    {
        var receipt = await _db.PurchaseReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.TenantId == tenantId);

        if (receipt is null)
            return new ReceiptActionResult(false, "RECEIPT_NOT_FOUND");

        if (receipt.Status != ReceiptStatus.DRAFT)
            return new ReceiptActionResult(false, "INVALID_STATUS");

        receipt.Status = ReceiptStatus.PENDING_PUTAWAY;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "CONFIRM", "purchase_receipt", receiptId, ip, ua);

        return new ReceiptActionResult(true, null);
    }

    public async Task<ReceiptActionResult> ProcessPutawayAsync(Guid tenantId, Guid receiptId, List<PutawayLineInput> putawayLines, Guid userId, string? ip, string? ua)
    {
        var receipt = await _db.PurchaseReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.TenantId == tenantId);

        if (receipt is null)
            return new ReceiptActionResult(false, "RECEIPT_NOT_FOUND");

        if (receipt.Status != ReceiptStatus.PENDING_PUTAWAY)
            return new ReceiptActionResult(false, receipt.Status == ReceiptStatus.COMPLETED ? "RECEIPT_ALREADY_COMPLETED" : "INVALID_STATUS");

        var transitLoc = await _db.Locations.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Code == "SUPPLIER-TRANSIT");
        if (transitLoc is null)
            return new ReceiptActionResult(false, "TRANSIT_LOCATION_NOT_FOUND");

        var now = DateTime.UtcNow;
        var transactionId = Guid.NewGuid();
        var ledgerEntries = new List<StockLedgerEntry>();

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var pl in putawayLines)
            {
                var receiptLine = receipt.Lines.FirstOrDefault(l => l.Id == pl.LineId);
                if (receiptLine is null)
                    return new ReceiptActionResult(false, $"LINE_NOT_FOUND: {pl.LineId}");

                var location = await _db.Locations.FirstOrDefaultAsync(l => l.Id == pl.LocationId && l.TenantId == tenantId);
                if (location is null)
                    return new ReceiptActionResult(false, $"LOCATION_NOT_FOUND: {pl.LocationId}");

                receiptLine.PutawayLocId = pl.LocationId;

                var goodQty = receiptLine.QtyPassed;
                if (goodQty > 0)
                {
                    ledgerEntries.Add(new StockLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transactionId,
                        ItemId = receiptLine.ItemId,
                        LocationId = pl.LocationId,
                        Quantity = goodQty,
                        UnitCost = 0,
                        ReferenceType = "PUTAWAY",
                        ReferenceId = receiptId,
                        TenantId = tenantId,
                        CreatedAt = now
                    });
                }

                var failedQty = receiptLine.QtyFailed;
                if (failedQty > 0)
                {
                    var quarantineLoc = await _db.Locations.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Type == LocationType.QUARANTINE);
                    if (quarantineLoc is null)
                        return new ReceiptActionResult(false, "QUARANTINE_LOCATION_NOT_FOUND");

                    ledgerEntries.Add(new StockLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transactionId,
                        ItemId = receiptLine.ItemId,
                        LocationId = quarantineLoc.Id,
                        Quantity = failedQty,
                        UnitCost = 0,
                        ReferenceType = "PUTAWAY",
                        ReferenceId = receiptId,
                        TenantId = tenantId,
                        CreatedAt = now
                    });

                    receiptLine.PutawayLocId = quarantineLoc.Id;
                }
            }

            foreach (var entry in ledgerEntries)
            {
                var creditEntry = new StockLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transactionId,
                    ItemId = entry.ItemId,
                    LocationId = transitLoc.Id,
                    Quantity = -entry.Quantity,
                    UnitCost = 0,
                    ReferenceType = "PUTAWAY",
                    ReferenceId = receiptId,
                    TenantId = tenantId,
                    CreatedAt = now
                };
                _db.StockLedgerEntries.Add(creditEntry);
                _db.StockLedgerEntries.Add(entry);

                var debitBalance = await _db.InventoryBalances
                    .FirstOrDefaultAsync(b => b.ItemId == entry.ItemId && b.LocationId == entry.LocationId && b.TenantId == tenantId);
                if (debitBalance is null)
                {
                    debitBalance = new InventoryBalance
                    {
                        Id = Guid.NewGuid(),
                        ItemId = entry.ItemId,
                        LocationId = entry.LocationId,
                        BalanceQty = entry.Quantity,
                        BalanceValue = 0,
                        TenantId = tenantId,
                        UpdatedAt = now
                    };
                    _db.InventoryBalances.Add(debitBalance);
                }
                else
                {
                    debitBalance.BalanceQty += entry.Quantity;
                    debitBalance.BalanceValue += 0;
                    debitBalance.UpdatedAt = now;
                }

                var creditBalance = await _db.InventoryBalances
                    .FirstOrDefaultAsync(b => b.ItemId == entry.ItemId && b.LocationId == transitLoc.Id && b.TenantId == tenantId);
                if (creditBalance is null)
                {
                    creditBalance = new InventoryBalance
                    {
                        Id = Guid.NewGuid(),
                        ItemId = entry.ItemId,
                        LocationId = transitLoc.Id,
                        BalanceQty = -entry.Quantity,
                        BalanceValue = 0,
                        TenantId = tenantId,
                        UpdatedAt = now
                    };
                    _db.InventoryBalances.Add(creditBalance);
                }
                else
                {
                    creditBalance.BalanceQty -= entry.Quantity;
                    creditBalance.BalanceValue += 0;
                    creditBalance.UpdatedAt = now;
                }
            }

            receipt.Status = ReceiptStatus.COMPLETED;
            await _db.SaveChangesAsync();

            foreach (var pl in putawayLines)
            {
                var receiptLine = receipt.Lines.First(l => l.Id == pl.LineId);
                var poLine = await _db.PurchaseOrderLines
                    .FirstOrDefaultAsync(ol => ol.Po != null && ol.Po.TenantId == tenantId && ol.Po.PoNumber == receipt.PoReference && ol.ItemId == receiptLine.ItemId);
                if (poLine is not null)
                {
                    poLine.ReceivedQty += receiptLine.QtyPassed;
                }
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(userId, tenantId, "PUTAWAY", "purchase_receipt", receiptId, ip, ua);

            var totalValue = receipt.Lines.Sum(l => l.QtyPassed);
            _dispatcher.Raise(new ReceiptProcessed(receiptId, totalValue, tenantId));

            await tx.CommitAsync();
            return new ReceiptActionResult(true, null);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static ReceiptDetail MapDetail(PurchaseReceipt r)
    {
        return new ReceiptDetail(
            r.Id,
            r.ReceiptNo,
            r.PoReference,
            r.Status.ToString(),
            r.ReceivedBy,
            r.CreatedAt,
            r.Lines.Select(l => new ReceiptLineDetail(
                l.Id,
                l.ItemId,
                l.Item?.Sku,
                l.Item?.Name,
                l.OrderedQty,
                l.QtyReceived,
                l.QtyPassed,
                l.QtyFailed,
                l.PutawayLocId,
                l.PutawayLoc?.Code
            )).ToList(),
            r.TenantId
        );
    }
}

public record ReceiptCreateResult(bool Success, Guid? ReceiptId, string? Error);
public record ReceiptLineInput(Guid ItemId, decimal QtyReceived, decimal QtyPassed, decimal QtyFailed);
public record ReceiptLineDetail(Guid Id, Guid ItemId, string? ItemSku, string? ItemName, decimal OrderedQty, decimal QtyReceived, decimal QtyPassed, decimal QtyFailed, Guid? PutawayLocId, string? PutawayLocCode);
public record ReceiptDetail(Guid Id, string ReceiptNo, string PoReference, string Status, string ReceivedBy, DateTime CreatedAt, List<ReceiptLineDetail> Lines, Guid TenantId);
public record ReceiptListResult(List<ReceiptDetail> Items, int Total, int Page, int PageSize);
public record ReceiptActionResult(bool Success, string? Error);
public record PutawayLineInput(Guid LineId, Guid LocationId);
