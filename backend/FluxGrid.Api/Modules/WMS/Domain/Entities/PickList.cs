using FluxGrid.Api.Modules.WMS.Domain.Enums;

namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class PickList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }
    public PickListStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PickListItem> Items { get; set; } = [];
}
