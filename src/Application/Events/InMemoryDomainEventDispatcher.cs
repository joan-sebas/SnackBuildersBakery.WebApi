using Domain;

namespace Application;

public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IReadOnlyDictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _handlers;

    public InMemoryDomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers)
    {
        var map = new Dictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>>();

        foreach (var handler in handlers)
        {
            foreach (var iface in handler.GetType().GetInterfaces())
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IDomainEventHandler<>))
                {
                    continue;
                }

                var eventType = iface.GetGenericArguments()[0];
                var method = iface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

                if (!map.TryGetValue(eventType, out var list))
                {
                    list = [];
                    map[eventType] = list;
                }

                // Capture the delegate once at construction; no per-call reflection.
                var captured = handler;
                var capturedMethod = method;
                list.Add((evt, ct) => (Task)capturedMethod.Invoke(captured, [evt, ct])!);
            }
        }

        _handlers = map;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(domainEvent.GetType(), out var handlers))
        {
            return;
        }

        foreach (var handle in handlers)
        {
            await handle(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DispatchAllAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
