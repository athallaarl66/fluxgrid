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

            db.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                PoNumber = "PO-9999",
                SupplierName = "Acme Supply Co.",
                PoDate = DateTime.UtcNow,
                TenantId = tenantId,
                Lines =
                [
                    new PurchaseOrderLine
                    {
                        Id = Guid.NewGuid(),
                        ItemId = sku001.Id,
                        OrderedQty = 100,
                        ReceivedQty = 0
                    }
                ]
            });
        }

        await db.SaveChangesAsync();
    }
}
