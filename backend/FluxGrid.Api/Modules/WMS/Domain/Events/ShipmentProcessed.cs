using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.WMS.Domain.Events;

public sealed record ShipmentProcessed(
    Guid OrderId,
    Guid ShipmentId,
    decimal TotalValue,
    decimal TotalCogs,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
