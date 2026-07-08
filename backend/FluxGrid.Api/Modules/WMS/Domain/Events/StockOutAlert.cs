using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.WMS.Domain.Events;

public class StockOutAlert : IDomainEvent
{
    public Guid ItemId { get; init; }
    public decimal CurrentStock { get; init; }
    public decimal ReorderPoint { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
