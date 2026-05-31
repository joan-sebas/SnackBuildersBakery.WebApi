using FluentAssertions;

namespace Domain.UnitTests.Scheduler;

public sealed class EstimatedReadyTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Bake = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Turnover = TimeSpan.FromMinutes(3);

    [Fact]
    public void Estimate_ForQueuedItemWithFreeSlot_ShouldBeNowPlusBakeTime()
    {
        var item = Queued(Now);
        var state = new SchedulerState(new[] { item }, new[] { FreeSlot(Now) });

        var estimates = CreateScheduler().EstimateReadyTimes(state, Now);

        estimates[item.OrderItemId].Should().Be(Now + Bake);
    }

    [Fact]
    public void Estimate_ForItemsBehindOthersOnOneSlot_ShouldAddBakeAndTurnoverCycles()
    {
        var first = Queued(Now);
        var second = Queued(Now.AddSeconds(1));
        var third = Queued(Now.AddSeconds(2));
        var state = new SchedulerState(new[] { first, second, third }, new[] { FreeSlot(Now) });

        var estimates = CreateScheduler().EstimateReadyTimes(state, Now);

        estimates[first.OrderItemId].Should().Be(Now + Bake);
        estimates[second.OrderItemId].Should().Be(Now + Bake + Turnover + Bake);
        estimates[third.OrderItemId].Should().Be(Now + Bake + Turnover + Bake + Turnover + Bake);
    }

    [Fact]
    public void Estimate_AcrossTwoFreeSlots_ShouldParallelizeThenSerialize()
    {
        var a = Queued(Now);
        var b = Queued(Now.AddSeconds(1));
        var c = Queued(Now.AddSeconds(2));
        var state = new SchedulerState(new[] { a, b, c }, new[] { FreeSlot(Now), FreeSlot(Now) });

        var estimates = CreateScheduler().EstimateReadyTimes(state, Now);

        estimates[a.OrderItemId].Should().Be(Now + Bake);
        estimates[b.OrderItemId].Should().Be(Now + Bake);
        estimates[c.OrderItemId].Should().Be(Now + Bake + Turnover + Bake);
    }

    [Fact]
    public void Estimate_ForQueuedItemBehindBakingSlot_ShouldWaitForSlotToFree()
    {
        var bakingEndsAt = Now.AddMinutes(2);
        var baking = Baking(bakingEndsAt);
        var queued = Queued(Now);
        var state = new SchedulerState(
            new[] { baking, queued },
            new[] { BakingSlot(baking.OrderItemId, bakingEndsAt) });

        var estimates = CreateScheduler().EstimateReadyTimes(state, Now);

        estimates[queued.OrderItemId].Should().Be(bakingEndsAt + Turnover + Bake);
    }

    [Fact]
    public void Estimate_ForBakingItem_ShouldReportBakingEndsAt()
    {
        var bakingEndsAt = Now.AddMinutes(4);
        var item = Baking(bakingEndsAt);
        var state = new SchedulerState(
            new[] { item },
            new[] { BakingSlot(item.OrderItemId, bakingEndsAt) });

        var estimates = CreateScheduler().EstimateReadyTimes(state, Now);

        estimates[item.OrderItemId].Should().Be(bakingEndsAt);
    }

    [Fact]
    public void Estimate_WhenHigherPriorityArrivesLater_ShouldGiveItTheEarlierSlot()
    {
        var walkIn = QueuedWith(PriorityLevel.WalkIn, Now.AddMinutes(-1));
        var vip = QueuedWith(PriorityLevel.Vip, Now);
        var state = new SchedulerState(new[] { walkIn, vip }, new[] { FreeSlot(Now) });

        var estimates = CreateScheduler().EstimateReadyTimes(state, Now);

        estimates[vip.OrderItemId].Should().Be(Now + Bake);
        estimates[walkIn.OrderItemId].Should().Be(Now + Bake + Turnover + Bake);
    }

    private static SchedulerItemState Queued(DateTimeOffset enqueuedAt)
        => QueuedWith(PriorityLevel.Vip, enqueuedAt);

    private static SchedulerItemState QueuedWith(PriorityLevel priority, DateTimeOffset enqueuedAt)
        => new(Guid.NewGuid(), priority, SnackType.Cookie, new Money(2.00m, "USD"),
            OrderItemStatus.Queued, enqueuedAt, null, null, null, null);

    private static SchedulerItemState Baking(DateTimeOffset bakingEndsAt)
        => new(Guid.NewGuid(), PriorityLevel.Vip, SnackType.Cookie, new Money(2.00m, "USD"),
            OrderItemStatus.Baking, bakingEndsAt.AddMinutes(-10), bakingEndsAt - Bake, bakingEndsAt, null, Guid.NewGuid());

    private static SchedulerSlotState FreeSlot(DateTimeOffset availableAt)
        => new(Guid.NewGuid(), Guid.NewGuid(), OvenSlotStatus.Free, availableAt, null, null);

    private static SchedulerSlotState BakingSlot(Guid itemId, DateTimeOffset bakingEndsAt)
        => new(Guid.NewGuid(), Guid.NewGuid(), OvenSlotStatus.Baking, bakingEndsAt, itemId, bakingEndsAt);

    private static KitchenScheduler CreateScheduler()
    {
        var provider = new TestSchedulerConfigProvider(
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
                Turnover: Turnover,
                new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = Bake }));
        return new KitchenScheduler(provider, new LinearAgingPolicy(provider));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
