using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.HR.Domain.Events;

public sealed record PayrollProcessed(
    Guid RunId,
    decimal TotalGross,
    decimal TotalTax,
    decimal TotalNet,
    string PeriodName,
    Guid TenantId,
    DateTime ProcessedDate
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
