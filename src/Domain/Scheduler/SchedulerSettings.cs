namespace Domain;

public sealed record SchedulerSettings(
    IReadOnlyDictionary<PriorityLevel, int> TierWeights,
    decimal AgingFactor,
    int Capacity,
    TimeSpan Turnover,
    IReadOnlyDictionary<SnackType, TimeSpan> BakeTimes);
