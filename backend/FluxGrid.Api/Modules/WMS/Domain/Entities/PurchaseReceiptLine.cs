namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class PurchaseReceiptLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReceiptId { get; set; }
    public PurchaseReceipt? Receipt { get; set; }
    public Guid ItemId { get; set; }
    public InventoryItem? Item { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal QtyReceived { get; set; }
    public decimal QtyPassed { get; set; }
    public decimal QtyFailed { get; set; }
    public Guid? PutawayLocId { get; set; }
    public Location? PutawayLoc { get; set; }
}
