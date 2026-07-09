using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.Application;

public class PurchaseOrderService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public PurchaseOrderService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PoCreateResult> CreatePoAsync(Guid tenantId, string poNumber, string supplierName, DateTime poDate, List<PoLineInput> lines, Guid userId, string? ip, string? ua)
    {
        if (await _db.PurchaseOrders.AnyAsync(p => p.TenantId == tenantId && p.PoNumber == poNumber))
            return new PoCreateResult(false, null, "PO_NUMBER_EXISTS");

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = poNumber,
            SupplierName = supplierName,
            PoDate = poDate,
            TenantId = tenantId,
            Lines = lines.Select(l => new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                ItemId = l.ItemId,
                OrderedQty = l.OrderedQty,
                ReceivedQty = 0
            }).ToList()
        };

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "CREATE", "purchase_order", po.Id, ip, ua);

        return new PoCreateResult(true, po.Id, null);
    }

    public async Task<PoDetail?> GetPoByIdAsync(Guid tenantId, Guid id)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (po is null) return null;

        return MapPoDetail(po);
    }

    public async Task<PoListResult> GetPoListAsync(Guid tenantId, string? search, int page, int pageSize)
    {
        var query = _db.PurchaseOrders.Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.PoNumber.Contains(search) || p.SupplierName.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .Include(p => p.Lines)
            .ThenInclude(l => l.Item)
            .OrderByDescending(p => p.PoDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PoListResult(items.Select(MapPoDetail).ToList(), total, page, pageSize);
    }

    private static PoDetail MapPoDetail(PurchaseOrder po)
    {
        return new PoDetail(
            po.Id,
            po.PoNumber,
            po.SupplierName,
            po.PoDate,
            po.Lines.Select(l => new PoLineDetail(
                l.Id,
                l.ItemId,
                l.Item?.Sku,
                l.Item?.Name,
                l.OrderedQty,
                l.ReceivedQty,
                l.OrderedQty - l.ReceivedQty
            )).ToList(),
            po.TenantId
        );
    }
}

public record PoCreateResult(bool Success, Guid? PoId, string? Error);
public record PoLineInput(Guid ItemId, decimal OrderedQty);
public record PoLineDetail(Guid Id, Guid ItemId, string? ItemSku, string? ItemName, decimal OrderedQty, decimal ReceivedQty, decimal OpenQty);
public record PoDetail(Guid Id, string PoNumber, string SupplierName, DateTime PoDate, List<PoLineDetail> Lines, Guid TenantId);
public record PoListResult(List<PoDetail> Items, int Total, int Page, int PageSize);
