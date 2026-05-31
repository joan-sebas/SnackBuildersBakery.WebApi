namespace Domain;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
