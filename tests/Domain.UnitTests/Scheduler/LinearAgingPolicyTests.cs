using FluentAssertions;

namespace Domain.UnitTests.Scheduler;

public sealed class LinearAgingPolicyTests
{
    [Fact]
    public void CalculateScore_WhenItemHasWaited_ShouldApplyTierWeightPlusAging()
    {
        var policy = new LinearAgingPolicy(CreateConfigProvider());
        var enqueuedAt = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var now = enqueuedAt.AddMinutes(10);

        var score = policy.CalculateScore(PriorityLevel.Delivery, enqueuedAt, now);

        score.Should().Be(30m);
    }

    [Fact]
    public void CalculateScore_WhenWaitIsZero_ShouldReturnTierWeight()
    {
        var policy = new LinearAgingPolicy(CreateConfigProvider());
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

        var score = policy.CalculateScore(PriorityLevel.WalkIn, now, now);

        score.Should().Be(10m);
    }

    [Fact]
    public void CalculateScore_WhenWaitIncreases_ShouldIncreaseMonotonically()
    {
        var policy = new LinearAgingPolicy(CreateConfigProvider());
        var enqueuedAt = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

        var shortWaitScore = policy.CalculateScore(PriorityLevel.WalkIn, enqueuedAt, enqueuedAt.AddMinutes(5));
        var longWaitScore = policy.CalculateScore(PriorityLevel.WalkIn, enqueuedAt, enqueuedAt.AddMinutes(15));

        longWaitScore.Should().BeGreaterThan(shortWaitScore);
    }

    [Fact]
    public void CalculateScore_WhenWaitIsEqual_ShouldPreserveTierOrdering()
    {
        var policy = new LinearAgingPolicy(CreateConfigProvider());
        var enqueuedAt = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var now = enqueuedAt.AddMinutes(5);

        var vipScore = policy.CalculateScore(PriorityLevel.Vip, enqueuedAt, now);
        var deliveryScore = policy.CalculateScore(PriorityLevel.Delivery, enqueuedAt, now);
        var walkInScore = policy.CalculateScore(PriorityLevel.WalkIn, enqueuedAt, now);

        vipScore.Should().BeGreaterThan(deliveryScore);
        deliveryScore.Should().BeGreaterThan(walkInScore);
    }

    private static ISchedulerConfigProvider CreateConfigProvider()
    {
        return new TestSchedulerConfigProvider(
            new SchedulerSettings(
                new Dictionary<PriorityLevel, int>
                {
                    [PriorityLevel.Vip] = 30,
                    [PriorityLevel.Delivery] = 20,
                    [PriorityLevel.WalkIn] = 10
                },
                AgingFactor: 1m,
                OvensCount: 2,
                TraysPerOven: 3,
                Turnover: TimeSpan.FromMinutes(2),
                new Dictionary<SnackType, TimeSpan>()));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
