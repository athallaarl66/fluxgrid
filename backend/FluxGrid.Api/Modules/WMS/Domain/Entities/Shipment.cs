namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShipmentNo { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public Guid TenantId { get; set; }
}
