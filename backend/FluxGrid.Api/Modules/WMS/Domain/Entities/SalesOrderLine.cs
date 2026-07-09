namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class SalesOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    public Guid ItemId { get; set; }
    public InventoryItem? Item { get; set; }
    public decimal QtyOrdered { get; set; }
    public decimal QtyReserved { get; set; }
    public decimal QtyPicked { get; set; }
    public decimal QtyShipped { get; set; }
}
