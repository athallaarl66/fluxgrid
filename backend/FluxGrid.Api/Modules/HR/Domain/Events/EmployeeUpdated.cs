using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.HR.Domain.Events;

public sealed record EmployeeUpdated(
    Guid EmployeeId,
    string EmployeeNo,
    string? PreviousJobTitle,
    Guid? PreviousDepartmentId,
    Guid? PreviousManagerId,
    Guid UpdatedBy,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
