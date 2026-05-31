namespace Domain;

public interface IAgingPolicy
{
    decimal CalculateScore(PriorityLevel priorityLevel, DateTimeOffset enqueuedAt, DateTimeOffset now);
}
