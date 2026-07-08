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
                new Location { Id = Guid.NewGuid(), Code = "WH-MAIN", Type = LocationType.WAREHOUSE, TenantId = tenantId }
            );
        }

        if (!await db.InventoryItems.AnyAsync())
        {
            db.InventoryItems.Add(
                new InventoryItem { Id = Guid.NewGuid(), Sku = "SKU-001", Name = "Safety Helmet", Uom = "pcs", TenantId = tenantId }
            );
        }

        await db.SaveChangesAsync();
    }
}
