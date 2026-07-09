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
                new Location { Id = Guid.NewGuid(), Code = "QUARANTINE", Type = LocationType.QUARANTINE, TenantId = tenantId },
                new Location { Id = Guid.NewGuid(), Code = "CUSTOMER-TRANSIT", Type = LocationType.TRANSIT, TenantId = tenantId }
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
            var sku001 = items.FirstOrDefault(i => i.Sku == "SKU-001");
            var sku002 = items.FirstOrDefault(i => i.Sku == "SKU-002");
            var whMain = await db.Locations.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Code == "WH-MAIN");
            var transit = await db.Locations.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Code == "SUPPLIER-TRANSIT");

            if (sku001 != null && sku002 != null && whMain != null && transit != null)
            {
                var poId = Guid.NewGuid();
                var po = new PurchaseOrder
                {
                    Id = poId,
                    PoNumber = "PO-9999",
                    SupplierName = "Acme Supply Co.",
                    PoDate = DateTime.UtcNow.AddDays(-7),
                    TenantId = tenantId,
                    Lines =
                    [
                        new PurchaseOrderLine { Id = Guid.NewGuid(), ItemId = sku001.Id, OrderedQty = 100, ReceivedQty = 100 },
                        new PurchaseOrderLine { Id = Guid.NewGuid(), ItemId = sku002.Id, OrderedQty = 50, ReceivedQty = 50 }
                    ]
                };
                db.PurchaseOrders.Add(po);

                var receiptId = Guid.NewGuid();
                var receipt = new PurchaseReceipt
                {
                    Id = receiptId,
                    ReceiptNo = "RCP-001",
                    PoReference = "PO-9999",
                    Status = ReceiptStatus.COMPLETED,
                    ReceivedBy = "System",
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    TenantId = tenantId,
                    Lines =
                    [
                        new PurchaseReceiptLine
                        {
                            Id = Guid.NewGuid(), ItemId = sku001.Id, OrderedQty = 100,
                            QtyReceived = 100, QtyPassed = 100, QtyFailed = 0,
                            PutawayLocId = whMain.Id
                        },
                        new PurchaseReceiptLine
                        {
                            Id = Guid.NewGuid(), ItemId = sku002.Id, OrderedQty = 50,
                            QtyReceived = 50, QtyPassed = 50, QtyFailed = 0,
                            PutawayLocId = whMain.Id
                        }
                    ]
                };
                db.PurchaseReceipts.Add(receipt);

                db.InventoryBalances.AddRange(
                    new InventoryBalance { Id = Guid.NewGuid(), ItemId = sku001.Id, LocationId = whMain.Id, BalanceQty = 100, BalanceValue = 0, TenantId = tenantId },
                    new InventoryBalance { Id = Guid.NewGuid(), ItemId = sku002.Id, LocationId = whMain.Id, BalanceQty = 50, BalanceValue = 0, TenantId = tenantId }
                );

                var putawayTx = Guid.NewGuid();
                db.StockLedgerEntries.AddRange(
                    new StockLedgerEntry { Id = Guid.NewGuid(), TransactionId = Guid.NewGuid(), ItemId = sku001.Id, LocationId = transit.Id, Quantity = 100, UnitCost = 0, ReferenceType = "RECEIPT", ReferenceId = receiptId, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new StockLedgerEntry { Id = Guid.NewGuid(), TransactionId = Guid.NewGuid(), ItemId = sku002.Id, LocationId = transit.Id, Quantity = 50, UnitCost = 0, ReferenceType = "RECEIPT", ReferenceId = receiptId, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new StockLedgerEntry { Id = Guid.NewGuid(), TransactionId = putawayTx, ItemId = sku001.Id, LocationId = transit.Id, Quantity = -100, UnitCost = 0, ReferenceType = "PUTAWAY", ReferenceId = receiptId, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new StockLedgerEntry { Id = Guid.NewGuid(), TransactionId = putawayTx, ItemId = sku001.Id, LocationId = whMain.Id, Quantity = 100, UnitCost = 0, ReferenceType = "PUTAWAY", ReferenceId = receiptId, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new StockLedgerEntry { Id = Guid.NewGuid(), TransactionId = putawayTx, ItemId = sku002.Id, LocationId = transit.Id, Quantity = -50, UnitCost = 0, ReferenceType = "PUTAWAY", ReferenceId = receiptId, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new StockLedgerEntry { Id = Guid.NewGuid(), TransactionId = putawayTx, ItemId = sku002.Id, LocationId = whMain.Id, Quantity = 50, UnitCost = 0, ReferenceType = "PUTAWAY", ReferenceId = receiptId, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-5) }
                );
            }
        }

        if (!await db.SalesOrders.AnyAsync())
        {
            var items = await db.InventoryItems.Where(i => i.TenantId == tenantId).ToListAsync();
            var sku001 = items.FirstOrDefault(i => i.Sku == "SKU-001");
            var sku002 = items.FirstOrDefault(i => i.Sku == "SKU-002");

            if (sku001 != null && sku002 != null)
            {
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
        }

        await db.SaveChangesAsync();
    }
}
