namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class PickListItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PickListId { get; set; }
    public PickList? PickList { get; set; }
    public Guid OrderLineId { get; set; }
    public SalesOrderLine? OrderLine { get; set; }
    public Guid ItemId { get; set; }
    public InventoryItem? Item { get; set; }
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public decimal QtyExpected { get; set; }
    public decimal QtyPicked { get; set; }
    public string? ShortPickReason { get; set; }
}
