namespace FluxGrid.Api.Shared.Domain.Events;

public sealed record AccountCreated(
    Guid AccountId,
    string Code,
    string Name,
    string Type,
    Guid? ParentId,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
