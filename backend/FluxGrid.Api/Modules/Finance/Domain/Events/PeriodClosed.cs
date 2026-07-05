using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.Finance.Domain.Events;

public sealed record PeriodClosed(
    Guid PeriodId,
    string PeriodName,
    DateTime StartDate,
    DateTime EndDate,
    Guid ClosedBy,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
