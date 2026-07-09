using FluxGrid.Api.Modules.WMS.Domain.Enums;

namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class SalesOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNo { get; set; } = string.Empty;
    public SalesOrderStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<SalesOrderLine> Lines { get; set; } = [];
}
