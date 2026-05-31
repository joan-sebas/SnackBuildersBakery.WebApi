namespace Domain;

public sealed record SchedulerSettings(
    IReadOnlyDictionary<PriorityLevel, int> TierWeights,
    decimal AgingFactor,
    int OvensCount,
    int TraysPerOven,
    TimeSpan Turnover,
    IReadOnlyDictionary<SnackType, TimeSpan> BakeTimes)
{
    public int Capacity => OvensCount * TraysPerOven;
}
