namespace Domain;

public sealed record OrderItemReady(
    Guid OrderId,
    Guid OrderItemId,
    DateTimeOffset OccurredAt) : IDomainEvent;
