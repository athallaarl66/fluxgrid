using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Shared.Infrastructure.Events;

public class DomainEventDispatcher
{
    private readonly List<IDomainEvent> _events = [];

    public void Raise(IDomainEvent domainEvent)
    {
        _events.Add(domainEvent);
    }

    public IReadOnlyList<IDomainEvent> GetEvents() => _events.AsReadOnly();

    public void Clear() => _events.Clear();
}
