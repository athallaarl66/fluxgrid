using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Modules.WMS.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.Application;

public class ShipmentService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly StockLedgerService _ledger;

    public ShipmentService(AppDbContext db, AuditService audit, DomainEventDispatcher dispatcher, StockLedgerService ledger)
    {
        _db = db;
        _audit = audit;
        _dispatcher = dispatcher;
        _ledger = ledger;
    }

    public async Task<VerifyResult> VerifyPackingAsync(Guid tenantId, Guid orderId, List<VerifyLineInput> lines, Guid userId, string? ip, string? ua)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order is null)
            return new VerifyResult(false, null, "ORDER_NOT_FOUND");

        var mismatches = new List<string>();
        foreach (var input in lines)
        {
            var orderLine = order.Lines.FirstOrDefault(ol => ol.ItemId == input.ItemId);
            if (orderLine is null)
            {
                mismatches.Add($"ITEM_NOT_ON_ORDER: {input.ItemId}");
                continue;
            }
            if (input.VerifiedQty != orderLine.QtyPicked)
                mismatches.Add($"PACKING_MISMATCH: item {input.ItemId} — picked {orderLine.QtyPicked}, verified {input.VerifiedQty}");
        }

        if (mismatches.Count > 0)
            return new VerifyResult(false, string.Join("; ", mismatches), "PACKING_MISMATCH");

        order.Status = SalesOrderStatus.PACKED;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "VERIFY", "sales_order", orderId, ip, ua);

        return new VerifyResult(true, null, null);
    }

    public async Task<ShipConfirmResult> ConfirmShipmentAsync(Guid tenantId, Guid orderId, Guid userId, string? ip, string? ua)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order is null)
            return new ShipConfirmResult(false, null, "ORDER_NOT_FOUND");

        if (order.Status != SalesOrderStatus.PACKED)
            return new ShipConfirmResult(false, null, "INVALID_STATUS_TRANSITION");

        if (await _db.Shipments.AnyAsync(s => s.OrderId == orderId && s.Status == "SHIPPED"))
            return new ShipConfirmResult(false, null, "SHIPMENT_ALREADY_CONFIRMED");

        var whMain = await _db.Locations.FirstAsync(l => l.TenantId == tenantId && l.Code == "WH-MAIN");
        var transitLoc = await _db.Locations.FirstAsync(l => l.TenantId == tenantId && l.Code == "SUPPLIER-TRANSIT");
        var now = DateTime.UtcNow;

        var nextNumber = await _db.Shipments.CountAsync() + 1;
        var shipmentNo = $"SHP-{now:yyyyMMdd}-{nextNumber:D4}";

        var totalValue = order.Lines.Sum(l => l.QtyShipped);
        var totalCogs = 0m;

        var movementEntries = new List<CreateMovementEntry>();
        foreach (var line in order.Lines)
        {
            var shippedQty = line.QtyPicked;
            if (shippedQty <= 0) continue;

            var avgCost = await _ledger.CalculateAverageCostValuationAsync(tenantId, line.ItemId);
            line.QtyShipped = shippedQty;
            line.QtyReserved = 0;

            movementEntries.Add(new CreateMovementEntry(line.ItemId, whMain.Id, -shippedQty, avgCost.AverageCost, "SHIPMENT", orderId));
            movementEntries.Add(new CreateMovementEntry(line.ItemId, transitLoc.Id, shippedQty, avgCost.AverageCost, "SHIPMENT", orderId));

            totalCogs += shippedQty * avgCost.AverageCost;
        }

        var movementResult = await _ledger.CreateMovementAsync(tenantId, movementEntries, userId, ip, ua);
        if (!movementResult.Success)
            return new ShipConfirmResult(false, null, movementResult.Error);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNo = shipmentNo,
            OrderId = orderId,
            Status = "SHIPPED",
            ShippedAt = now,
            TenantId = tenantId
        };

        order.Status = SalesOrderStatus.SHIPPED;
        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "SHIP", "shipment", shipment.Id, ip, ua);
        _dispatcher.Raise(new ShipmentProcessed(orderId, shipment.Id, totalValue, totalCogs, tenantId));

        return new ShipConfirmResult(true, shipment.Id, null);
    }

    public async Task<ShipListResult> GetShipmentListAsync(Guid tenantId, Guid? orderId, int page, int pageSize)
    {
        var query = _db.Shipments.Where(s => s.TenantId == tenantId);

        if (orderId.HasValue)
            query = query.Where(s => s.OrderId == orderId.Value);

        var total = await query.CountAsync();
        var items = await query
            .Include(s => s.Order)
            .OrderByDescending(s => s.ShippedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ShipListResult(items.Select(s => new ShipDetail(
            s.Id,
            s.ShipmentNo,
            s.OrderId,
            s.Order?.OrderNo,
            s.Status,
            s.ShippedAt,
            s.TenantId
        )).ToList(), total, page, pageSize);
    }
}

public record VerifyResult(bool Success, string? ErrorDetail, string? Error);
public record VerifyLineInput(Guid ItemId, decimal VerifiedQty);
public record ShipConfirmResult(bool Success, Guid? ShipmentId, string? Error);
public record ShipDetail(Guid Id, string ShipmentNo, Guid OrderId, string? OrderNo, string Status, DateTime? ShippedAt, Guid TenantId);
public record ShipListResult(List<ShipDetail> Items, int Total, int Page, int PageSize);
