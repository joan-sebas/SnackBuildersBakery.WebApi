using Domain;
using Microsoft.Extensions.Options;

namespace Infrastructure;

/// <summary>
/// Bridges IOptions configuration to the domain's ISchedulerConfigProvider contract.
/// Dictionary keys use enum names as strings to stay JSON-friendly.
/// </summary>
internal sealed class OptionsSchedulerConfigProvider(IOptions<SchedulerOptions> options)
    : ISchedulerConfigProvider
{
    public SchedulerSettings Settings => Build(options.Value);

    private static SchedulerSettings Build(SchedulerOptions o) => new(
        TierWeights: o.TierWeights.ToDictionary(
            kv => Enum.Parse<PriorityLevel>(kv.Key, ignoreCase: true),
            kv => kv.Value),
        AgingFactor: o.AgingFactor,
        OvensCount: o.OvensCount,
        TraysPerOven: o.TraysPerOven,
        Turnover: TimeSpan.Parse(o.Turnover),
        BakeTimes: o.BakeTimes.ToDictionary(
            kv => Enum.Parse<SnackType>(kv.Key, ignoreCase: true),
            kv => TimeSpan.Parse(kv.Value)));
}

public sealed class SchedulerOptions
{
    public Dictionary<string, int> TierWeights { get; set; } = [];
    public decimal AgingFactor { get; set; }
    public int OvensCount { get; set; }
    public int TraysPerOven { get; set; }
    public string Turnover { get; set; } = string.Empty;
    public Dictionary<string, string> BakeTimes { get; set; } = [];
}
