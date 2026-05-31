using FluentAssertions;

namespace Domain.UnitTests.Scheduler;

public sealed class KitchenQueueTests
{
    [Fact]
    public void SelectNext_WhenQueueIsEmpty_ShouldReturnNull()
    {
        var queue = new KitchenQueue(new ConstantAgingPolicy(0m));

        var next = queue.SelectNext(new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero));

        next.Should().BeNull();
    }

    [Fact]
    public void EnqueueAndRemove_WhenItemIsQueued_ShouldUpdateCount()
    {
        var queue = new KitchenQueue(new ConstantAgingPolicy(0m));
        var item = CreateOrderItem(PriorityLevel.WalkIn, new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero));

        queue.Enqueue(item);
        queue.Remove(item);

        queue.Count.Should().Be(0);
    }

    [Fact]
    public void SelectNext_WhenScoresDiffer_ShouldReturnHighestScore()
    {
        var policy = new LinearAgingPolicy(CreateConfigProvider());
        var queue = new KitchenQueue(policy);
        var now = new DateTimeOffset(2026, 5, 31, 13, 30, 0, TimeSpan.Zero);
        var olderWalkIn = CreateOrderItem(PriorityLevel.WalkIn, now.AddMinutes(-30));
        var newerVip = CreateOrderItem(PriorityLevel.Vip, now);

        queue.Enqueue(olderWalkIn);
        queue.Enqueue(newerVip);

        queue.SelectNext(now).Should().Be(olderWalkIn);
    }

    [Fact]
    public void SelectNext_WhenScoreTies_ShouldReturnHigherBaseTier()
    {
        var queue = new KitchenQueue(new ConstantAgingPolicy(10m));
        var now = new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero);
        var walkIn = CreateOrderItem(PriorityLevel.WalkIn, now);
        var vip = CreateOrderItem(PriorityLevel.Vip, now);

        queue.Enqueue(walkIn);
        queue.Enqueue(vip);

        queue.SelectNext(now).Should().Be(vip);
    }

    [Fact]
    public void SelectNext_WhenScoreAndTierTie_ShouldReturnEarliestEnqueuedItem()
    {
        var queue = new KitchenQueue(new ConstantAgingPolicy(10m));
        var now = new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero);
        var older = CreateOrderItem(PriorityLevel.Delivery, now.AddMinutes(-5));
        var newer = CreateOrderItem(PriorityLevel.Delivery, now);

        queue.Enqueue(newer);
        queue.Enqueue(older);

        queue.SelectNext(now).Should().Be(older);
    }

    [Fact]
    public void SelectNext_WhenCalled_ShouldScoreEveryCandidateAgainstNow()
    {
        var policy = new CountingAgingPolicy();
        var queue = new KitchenQueue(policy);
        var now = new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero);

        queue.Enqueue(CreateOrderItem(PriorityLevel.Vip, now));
        queue.Enqueue(CreateOrderItem(PriorityLevel.Delivery, now));
        queue.Enqueue(CreateOrderItem(PriorityLevel.WalkIn, now));

        queue.SelectNext(now);

        policy.CallCount.Should().Be(3);
    }

    private static OrderItem CreateOrderItem(PriorityLevel priorityLevel, DateTimeOffset enqueuedAt)
    {
        var menuItem = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"));
        var item = new OrderItem(Guid.NewGuid(), menuItem, enqueuedAt);
        item.AssignPriority(priorityLevel);
        return item;
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

    private sealed class CountingAgingPolicy : IAgingPolicy
    {
        public int CallCount { get; private set; }

        public decimal CalculateScore(PriorityLevel priorityLevel, DateTimeOffset enqueuedAt, DateTimeOffset now)
        {
            CallCount++;
            return 1m;
        }
    }
}
