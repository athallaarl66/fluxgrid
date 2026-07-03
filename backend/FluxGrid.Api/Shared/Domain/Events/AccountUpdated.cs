namespace FluxGrid.Api.Shared.Domain.Events;

public sealed record AccountUpdated(
    Guid AccountId,
    string Code,
    string Name,
    string Type,
    bool IsActive,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
