using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class WmsDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (!await db.Locations.AnyAsync())
        {
            db.Locations.AddRange(
                new Location { Id = Guid.NewGuid(), Code = "SUPPLIER-TRANSIT", Type = LocationType.TRANSIT, TenantId = tenantId },
                new Location { Id = Guid.NewGuid(), Code = "WH-MAIN", Type = LocationType.WAREHOUSE, TenantId = tenantId },
                new Location { Id = Guid.NewGuid(), Code = "QUARANTINE", Type = LocationType.QUARANTINE, TenantId = tenantId }
            );
        }

        if (!await db.InventoryItems.AnyAsync())
        {
            db.InventoryItems.AddRange(
                new InventoryItem { Id = Guid.NewGuid(), Sku = "SKU-001", Name = "Safety Helmet", Uom = "pcs", TenantId = tenantId },
                new InventoryItem { Id = Guid.NewGuid(), Sku = "SKU-002", Name = "Work Gloves", Uom = "pair", TenantId = tenantId }
            );
        }

        if (!await db.PurchaseOrders.AnyAsync())
        {
            await db.SaveChangesAsync();

            var items = await db.InventoryItems.Where(i => i.TenantId == tenantId).ToListAsync();
            var sku001 = items.First(i => i.Sku == "SKU-001");
            var sku002 = items.First(i => i.Sku == "SKU-002");
            var whMain = await db.Locations.FirstAsync(l => l.TenantId == tenantId && l.Code == "WH-MAIN");

            db.InventoryBalances.AddRange(
                new InventoryBalance { Id = Guid.NewGuid(), ItemId = sku001.Id, LocationId = whMain.Id, BalanceQty = 200, BalanceValue = 0, TenantId = tenantId },
                new InventoryBalance { Id = Guid.NewGuid(), ItemId = sku002.Id, LocationId = whMain.Id, BalanceQty = 150, BalanceValue = 0, TenantId = tenantId }
            );

            await db.SaveChangesAsync();

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                OrderNo = "SO-123",
                Status = SalesOrderStatus.PENDING,
                CustomerId = Guid.NewGuid(),
                CustomerName = "Test Customer",
                TenantId = tenantId,
                Lines =
                [
                    new SalesOrderLine { Id = Guid.NewGuid(), ItemId = sku001.Id, QtyOrdered = 5, QtyReserved = 0, QtyPicked = 0, QtyShipped = 0 },
                    new SalesOrderLine { Id = Guid.NewGuid(), ItemId = sku002.Id, QtyOrdered = 10, QtyReserved = 0, QtyPicked = 0, QtyShipped = 0 }
                ]
            };
            db.SalesOrders.Add(order);
        }

        await db.SaveChangesAsync();
    }
}
