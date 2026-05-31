using FluentAssertions;
using FsCheck.Xunit;

namespace Domain.UnitTests.Scheduler;

public sealed class SelectionPropertyTests
{
    [Property(MaxTest = 100)]
    public void CalculateScore_WhenLowerTierWaitsLongEnough_ShouldOvertakeHigherTier(int scoreGap, int extraWait)
    {
        var normalizedGap = Math.Abs(scoreGap % 500) + 1;
        var normalizedExtraWait = Math.Abs(extraWait % 500) + 1;
        var agingFactor = 1m;
        var lowerWeight = 10;
        var higherWeight = lowerWeight + normalizedGap;
        var waitedMinutes = normalizedGap + normalizedExtraWait;
        var now = new DateTimeOffset(2026, 5, 31, 14, 0, 0, TimeSpan.Zero);
        var provider = CreateConfigProvider(
            vipWeight: higherWeight,
            deliveryWeight: lowerWeight,
            walkInWeight: lowerWeight,
            agingFactor);
        var policy = new LinearAgingPolicy(provider);

        var higherTierScore = policy.CalculateScore(PriorityLevel.Vip, now, now);
        var lowerTierScore = policy.CalculateScore(PriorityLevel.Delivery, now.AddMinutes(-waitedMinutes), now);

        lowerTierScore.Should().BeGreaterThan(higherTierScore);
    }

    [Property(MaxTest = 100)]
    public void SelectNext_WhenScoresTie_ShouldPreferHigherBaseTier(int minutesAgo)
    {
        var normalizedMinutesAgo = Math.Abs(minutesAgo % 500);
        var now = new DateTimeOffset(2026, 5, 31, 14, 0, 0, TimeSpan.Zero);
        var queue = new KitchenQueue(new ConstantAgingPolicy(100m));
        var lowerTier = CreateOrderItem(PriorityLevel.WalkIn, now.AddMinutes(-normalizedMinutesAgo));
        var higherTier = CreateOrderItem(PriorityLevel.Vip, now.AddMinutes(-normalizedMinutesAgo));

        queue.Enqueue(lowerTier);
        queue.Enqueue(higherTier);

        queue.SelectNext(now).Should().Be(higherTier);
    }

    [Property(MaxTest = 100)]
    public void SelectNext_WhenScoreAndTierTie_ShouldPreferEarliestEnqueuedItem(int olderWait, int newerWait)
    {
        var normalizedOlderWait = Math.Abs(olderWait % 500) + 2;
        var normalizedNewerWait = Math.Abs(newerWait % normalizedOlderWait);
        var now = new DateTimeOffset(2026, 5, 31, 14, 0, 0, TimeSpan.Zero);
        var queue = new KitchenQueue(new ConstantAgingPolicy(100m));
        var older = CreateOrderItem(PriorityLevel.Delivery, now.AddMinutes(-normalizedOlderWait));
        var newer = CreateOrderItem(PriorityLevel.Delivery, now.AddMinutes(-normalizedNewerWait));

        queue.Enqueue(newer);
        queue.Enqueue(older);

        queue.SelectNext(now).Should().Be(older);
    }

    private static OrderItem CreateOrderItem(PriorityLevel priorityLevel, DateTimeOffset enqueuedAt)
    {
        var menuItem = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"));
        var item = new OrderItem(Guid.NewGuid(), menuItem, enqueuedAt);
        item.AssignPriority(priorityLevel);
        return item;
    }

    private static ISchedulerConfigProvider CreateConfigProvider(
        int vipWeight,
        int deliveryWeight,
        int walkInWeight,
        decimal agingFactor)
    {
        return new TestSchedulerConfigProvider(
            new SchedulerSettings(
                new Dictionary<PriorityLevel, int>
                {
                    [PriorityLevel.Vip] = vipWeight,
                    [PriorityLevel.Delivery] = deliveryWeight,
                    [PriorityLevel.WalkIn] = walkInWeight
                },
                agingFactor,
                Capacity: 4,
                Turnover: TimeSpan.FromMinutes(2),
                new Dictionary<SnackType, TimeSpan>()));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }

    private sealed class ConstantAgingPolicy(decimal score) : IAgingPolicy
    {
        public decimal CalculateScore(PriorityLevel priorityLevel, DateTimeOffset enqueuedAt, DateTimeOffset now)
        {
            return score;
        }
    }
}
