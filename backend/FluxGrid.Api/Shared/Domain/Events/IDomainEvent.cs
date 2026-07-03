namespace FluxGrid.Api.Shared.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
