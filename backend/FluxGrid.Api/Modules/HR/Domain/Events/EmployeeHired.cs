using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.HR.Domain.Events;

public sealed record EmployeeHired(
    Guid EmployeeId,
    string EmployeeNo,
    string FirstName,
    string LastName,
    Guid? DepartmentId,
    Guid? ManagerId,
    string? JobTitle,
    Guid CreatedBy,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
