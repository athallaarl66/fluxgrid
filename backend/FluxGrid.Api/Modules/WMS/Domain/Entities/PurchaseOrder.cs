namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PoNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PoDate { get; set; }
    public Guid TenantId { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];
}
