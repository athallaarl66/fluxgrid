using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.Finance.Domain.Events;

public sealed record PeriodReopened(
    Guid PeriodId,
    string PeriodName,
    DateTime StartDate,
    DateTime EndDate,
    string Reason,
    Guid ReopenedBy,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
