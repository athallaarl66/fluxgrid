namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class InventoryBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid LocationId { get; set; }
    public decimal BalanceQty { get; set; }
    public decimal BalanceValue { get; set; }
    public Guid TenantId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
