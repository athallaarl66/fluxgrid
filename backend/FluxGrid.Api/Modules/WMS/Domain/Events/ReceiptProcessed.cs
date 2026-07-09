using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.WMS.Domain.Events;

public sealed record ReceiptProcessed(
    Guid ReceiptId,
    decimal TotalValue,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
