using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.Application;

public class SalesOrderService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public SalesOrderService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<SoCreateResult> CreateOrderAsync(Guid tenantId, string orderNo, Guid customerId, string customerName, string? notes, List<SoLineInput> lines, Guid userId, string? ip, string? ua)
    {
        if (await _db.SalesOrders.AnyAsync(o => o.TenantId == tenantId && o.OrderNo == orderNo))
            return new SoCreateResult(false, null, "DUPLICATE_ORDER_NO");

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = orderNo,
            Status = SalesOrderStatus.PENDING,
            CustomerId = customerId,
            CustomerName = customerName,
            Notes = notes,
            TenantId = tenantId,
            Lines = lines.Select(l => new SalesOrderLine
            {
                Id = Guid.NewGuid(),
                ItemId = l.ItemId,
                QtyOrdered = l.QtyOrdered,
                QtyReserved = 0,
                QtyPicked = 0,
                QtyShipped = 0
            }).ToList()
        };

        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "CREATE", "sales_order", order.Id, ip, ua);

        return new SoCreateResult(true, order.Id, null);
    }

    public async Task<SoDetail?> GetOrderAsync(Guid tenantId, Guid id)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (order is null) return null;
        return MapDetail(order);
    }

    public async Task<SoListResult> GetOrderListAsync(Guid tenantId, string? search, string? status, int page, int pageSize)
    {
        var query = _db.SalesOrders.Where(o => o.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.OrderNo.Contains(search) || o.CustomerName.Contains(search));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<SalesOrderStatus>(status, true, out var parsed))
            query = query.Where(o => o.Status == parsed);

        var total = await query.CountAsync();
        var items = await query
            .Include(o => o.Lines)
            .ThenInclude(l => l.Item)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new SoListResult(items.Select(MapDetail).ToList(), total, page, pageSize);
    }

    public async Task<SoActionResult> CancelOrderAsync(Guid tenantId, Guid id, Guid userId, string? ip, string? ua)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (order is null)
            return new SoActionResult(false, "ORDER_NOT_FOUND");

        if (order.Status == SalesOrderStatus.SHIPPED)
            return new SoActionResult(false, "INVALID_STATUS_TRANSITION");

        foreach (var line in order.Lines)
        {
            line.QtyReserved = 0;
        }

        order.Status = SalesOrderStatus.CANCELLED;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "CANCEL", "sales_order", id, ip, ua);

        return new SoActionResult(true, null);
    }

    private static SoDetail MapDetail(SalesOrder o)
    {
        return new SoDetail(
            o.Id,
            o.OrderNo,
            o.Status.ToString(),
            o.CustomerId,
            o.CustomerName,
            o.Notes,
            o.CreatedAt,
            o.Lines.Select(l => new SoLineDetail(
                l.Id,
                l.ItemId,
                l.Item?.Sku,
                l.Item?.Name,
                l.QtyOrdered,
                l.QtyReserved,
                l.QtyPicked,
                l.QtyShipped
            )).ToList(),
            o.TenantId
        );
    }
}

public record SoCreateResult(bool Success, Guid? OrderId, string? Error);
public record SoLineInput(Guid ItemId, decimal QtyOrdered);
public record SoLineDetail(Guid Id, Guid ItemId, string? ItemSku, string? ItemName, decimal QtyOrdered, decimal QtyReserved, decimal QtyPicked, decimal QtyShipped);
public record SoDetail(Guid Id, string OrderNo, string Status, Guid CustomerId, string CustomerName, string? Notes, DateTime CreatedAt, List<SoLineDetail> Lines, Guid TenantId);
public record SoListResult(List<SoDetail> Items, int Total, int Page, int PageSize);
public record SoActionResult(bool Success, string? Error);
