using Domain;

namespace Application;

/// <summary>Non-generic marker used for registration collections.</summary>
public interface IDomainEventHandler;

/// <summary>Handles a specific domain event type.</summary>
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
