namespace Domain;

public sealed record PaymentSucceeded(
    Guid OrderId,
    Guid PaymentId,
    DateTimeOffset OccurredAt) : IDomainEvent;
