using Domain;

namespace Application;

/// <summary>
/// Dispatches domain events to registered handlers after the unit of work commits.
/// Delivery is best-effort at-most-once: an in-process crash between commit and dispatch
/// loses the events. A transactional outbox is the documented upgrade path when stronger
/// guarantees are required.
/// Handler exceptions propagate to the caller; no events are swallowed silently.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    Task DispatchAllAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
