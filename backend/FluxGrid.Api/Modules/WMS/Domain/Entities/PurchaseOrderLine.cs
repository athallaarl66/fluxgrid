namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class PurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoId { get; set; }
    public PurchaseOrder? Po { get; set; }
    public Guid ItemId { get; set; }
    public InventoryItem? Item { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
}
