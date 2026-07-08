namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}
