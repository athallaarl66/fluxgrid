using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.WMS.Domain.Events;

public class StockMovement : IDomainEvent
{
    public Guid ItemId { get; init; }
    public Guid LocationId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string ReferenceType { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
