namespace Domain;

public sealed class LinearAgingPolicy(ISchedulerConfigProvider configProvider) : IAgingPolicy
{
    public decimal CalculateScore(PriorityLevel priorityLevel, DateTimeOffset enqueuedAt, DateTimeOffset now)
    {
        var waitedMinutes = (decimal)(now - enqueuedAt).TotalMinutes;
        var tierWeight = configProvider.Settings.TierWeights[priorityLevel];

        return tierWeight + configProvider.Settings.AgingFactor * waitedMinutes;
    }
}
