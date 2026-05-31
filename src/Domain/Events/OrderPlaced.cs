namespace Domain;

public sealed record OrderPlaced(
    Guid OrderId,
    PriorityLevel PriorityLevel,
    DateTimeOffset OccurredAt) : IDomainEvent;
