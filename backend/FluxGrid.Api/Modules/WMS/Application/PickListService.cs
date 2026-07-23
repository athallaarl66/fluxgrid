using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.Application;

public class PickListService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public PickListService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PickListCreateResult> GeneratePickListAsync(Guid tenantId, Guid orderId, Guid userId, string? ip, string? ua)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order is null)
            return new PickListCreateResult(false, null, "ORDER_NOT_FOUND");

        if (await _db.PickLists.AnyAsync(pl => pl.OrderId == orderId && pl.Status != PickListStatus.CANCELLED))
            return new PickListCreateResult(false, null, "DUPLICATE_PICK_LIST");

        using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var pickItems = new List<PickListItem>();
            foreach (var line in order.Lines)
            {
                var available = await _db.InventoryBalances
                    .Where(b => b.ItemId == line.ItemId && b.TenantId == tenantId)
                    .SumAsync(b => b.BalanceQty);

                var reservedOnOthers = await _db.PickLists
                    .Where(pl => pl.OrderId != orderId && pl.Status != PickListStatus.CANCELLED)
                    .SelectMany(pl => pl.Items)
                    .Where(pi => pi.ItemId == line.ItemId)
                    .SumAsync(pi => pi.QtyExpected - pi.QtyPicked);

                var effectiveAvailable = available - reservedOnOthers;

                if (effectiveAvailable < line.QtyOrdered - line.QtyReserved)
                    return new PickListCreateResult(false, null, $"INSUFFICIENT_STOCK: item {line.ItemId}");

                var whMain = await _db.Locations.FirstAsync(l => l.TenantId == tenantId && l.Code == "WH-MAIN");

                pickItems.Add(new PickListItem
                {
                    Id = Guid.NewGuid(),
                    ItemId = line.ItemId,
                    OrderLineId = line.Id,
                    LocationId = whMain.Id,
                    QtyExpected = line.QtyOrdered - line.QtyReserved,
                    QtyPicked = 0
                });
            }

            var pickList = new PickList
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Status = PickListStatus.GENERATED,
                TenantId = tenantId,
                Items = pickItems
            };

            foreach (var line in order.Lines)
            {
                line.QtyReserved = line.QtyOrdered;
            }

            order.Status = SalesOrderStatus.RESERVED;

            _db.PickLists.Add(pickList);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync(userId, tenantId, "GENERATE", "pick_list", pickList.Id, ip, ua);

            return new PickListCreateResult(true, pickList.Id, null);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<PickListDetail?> GetPickListAsync(Guid tenantId, Guid id)
    {
        var pickList = await _db.PickLists
            .Include(pl => pl.Items)
            .ThenInclude(pi => pi.Item)
            .Include(pl => pl.Items)
            .ThenInclude(pi => pi.Location)
            .Include(pl => pl.Order)
            .FirstOrDefaultAsync(pl => pl.Id == id && pl.TenantId == tenantId);

        if (pickList is null) return null;

        return new PickListDetail(
            pickList.Id,
            pickList.OrderId,
            pickList.Order?.OrderNo,
            pickList.Status.ToString(),
            pickList.AssignedTo,
            pickList.CreatedAt,
            pickList.Items.Select(pi => new PickItemDetail(
                pi.Id,
                pi.OrderLineId,
                pi.ItemId,
                pi.Item?.Sku,
                pi.Item?.Name,
                pi.LocationId,
                pi.Location?.Code,
                pi.QtyExpected,
                pi.QtyPicked,
                pi.ShortPickReason
            )).ToList(),
            pickList.TenantId
        );
    }

    public async Task<PickListDetail?> GetPickListByOrderAsync(Guid tenantId, Guid orderId)
    {
        var pickList = await _db.PickLists
            .Include(pl => pl.Items)
            .ThenInclude(pi => pi.Item)
            .Include(pl => pl.Items)
            .ThenInclude(pi => pi.Location)
            .Include(pl => pl.Order)
            .FirstOrDefaultAsync(pl => pl.OrderId == orderId && pl.TenantId == tenantId
                && pl.Status != PickListStatus.CANCELLED);

        if (pickList is null) return null;

        return new PickListDetail(
            pickList.Id,
            pickList.OrderId,
            pickList.Order?.OrderNo,
            pickList.Status.ToString(),
            pickList.AssignedTo,
            pickList.CreatedAt,
            pickList.Items.Select(pi => new PickItemDetail(
                pi.Id,
                pi.OrderLineId,
                pi.ItemId,
                pi.Item?.Sku,
                pi.Item?.Name,
                pi.LocationId,
                pi.Location?.Code,
                pi.QtyExpected,
                pi.QtyPicked,
                pi.ShortPickReason
            )).ToList(),
            pickList.TenantId
        );
    }

    public async Task<PickListActionResult> ExecutePickItemsAsync(Guid tenantId, Guid pickListId, List<PickExecutionInput> items, Guid userId, string? ip, string? ua)
    {
        var pickList = await _db.PickLists
            .Include(pl => pl.Items)
            .Include(pl => pl.Order)
            .ThenInclude(o => o!.Lines)
            .FirstOrDefaultAsync(pl => pl.Id == pickListId && pl.TenantId == tenantId);

        if (pickList is null)
            return new PickListActionResult(false, "PICK_LIST_NOT_FOUND");

        if (pickList.Status == PickListStatus.COMPLETED || pickList.Status == PickListStatus.CANCELLED)
            return new PickListActionResult(false, "INVALID_STATUS");

        foreach (var input in items)
        {
            var item = pickList.Items.FirstOrDefault(pi => pi.Id == input.ItemId);
            if (item is null)
                return new PickListActionResult(false, $"ITEM_NOT_FOUND: {input.ItemId}");

            if (input.QtyPicked < item.QtyExpected && string.IsNullOrEmpty(input.ShortPickReason))
                return new PickListActionResult(false, "SHORT_PICK_REASON_REQUIRED");

            item.QtyPicked = input.QtyPicked;
            item.ShortPickReason = input.ShortPickReason;

            var orderLine = pickList.Order!.Lines.FirstOrDefault(ol => ol.Id == item.OrderLineId);
            if (orderLine is not null)
                orderLine.QtyPicked = input.QtyPicked;
        }

        var allComplete = pickList.Items.All(pi => pi.QtyPicked >= pi.QtyExpected);
        pickList.Status = allComplete ? PickListStatus.COMPLETED : PickListStatus.IN_PROGRESS;

        if (allComplete && pickList.Order is not null)
            pickList.Order.Status = SalesOrderStatus.PICKING;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "EXECUTE_PICK", "pick_list", pickListId, ip, ua);

        return new PickListActionResult(true, null);
    }

    public async Task<PickListActionResult> CancelPickListAsync(Guid tenantId, Guid pickListId, Guid userId, string? ip, string? ua)
    {
        var pickList = await _db.PickLists
            .Include(pl => pl.Items)
            .Include(pl => pl.Order)
            .ThenInclude(o => o!.Lines)
            .FirstOrDefaultAsync(pl => pl.Id == pickListId && pl.TenantId == tenantId);

        if (pickList is null)
            return new PickListActionResult(false, "PICK_LIST_NOT_FOUND");

        if (pickList.Status == PickListStatus.COMPLETED || pickList.Status == PickListStatus.CANCELLED)
            return new PickListActionResult(false, "INVALID_STATUS");

        pickList.Status = PickListStatus.CANCELLED;

        if (pickList.Order is not null)
        {
            foreach (var line in pickList.Order.Lines)
            {
                line.QtyReserved = 0;
            }
            pickList.Order.Status = SalesOrderStatus.PENDING;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, tenantId, "CANCEL", "pick_list", pickListId, ip, ua);

        return new PickListActionResult(true, null);
    }
}

public record PickListCreateResult(bool Success, Guid? PickListId, string? Error);
public record PickListDetail(Guid Id, Guid OrderId, string? OrderNo, string Status, string? AssignedTo, DateTime CreatedAt, List<PickItemDetail> Items, Guid TenantId);
public record PickItemDetail(Guid Id, Guid OrderLineId, Guid ItemId, string? ItemSku, string? ItemName, Guid? LocationId, string? LocationCode, decimal QtyExpected, decimal QtyPicked, string? ShortPickReason);
public record PickListActionResult(bool Success, string? Error);
public record PickExecutionInput(Guid ItemId, decimal QtyPicked, string? ShortPickReason);
