using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.HR.Domain.Events;

public sealed record EmployeeTerminated(
    Guid EmployeeId,
    string EmployeeNo,
    Guid? UserId,
    DateTime TerminationDate,
    Guid TerminatedBy,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
