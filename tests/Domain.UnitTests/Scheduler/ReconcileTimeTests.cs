using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Domain.UnitTests.Scheduler;

public sealed class ReconcileTimeTests
{
    private static readonly DateTimeOffset Start = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan BakeTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Turnover = TimeSpan.FromMinutes(3);

    [Fact]
    public void Reconcile_WhenNowAdvances_ShouldDriveBakeReadyTurnoverThenAssignNext()
    {
        var timeProvider = new FakeTimeProvider(Start);
        var scheduler = CreateScheduler();
        var vipId = Guid.NewGuid();
        var walkInId = Guid.NewGuid();
        var state = new SchedulerState(
            new[]
            {
                CreateQueuedItem(vipId, PriorityLevel.Vip, Start),
                CreateQueuedItem(walkInId, PriorityLevel.WalkIn, Start)
            },
            new[] { CreateFreeSlot(Start) });

        state = scheduler.Reconcile(state, timeProvider.GetUtcNow());
        Find(state, vipId).Status.Should().Be(OrderItemStatus.Baking);
        Find(state, walkInId).Status.Should().Be(OrderItemStatus.Queued);

        timeProvider.Advance(BakeTime);
        state = scheduler.Reconcile(state, timeProvider.GetUtcNow());
        Find(state, vipId).Status.Should().Be(OrderItemStatus.Ready);
        state.Slots[0].Status.Should().Be(OvenSlotStatus.Turnover);
        Find(state, walkInId).Status.Should().Be(OrderItemStatus.Queued);

        timeProvider.Advance(Turnover);
        state = scheduler.Reconcile(state, timeProvider.GetUtcNow());
        state.Slots[0].Status.Should().Be(OvenSlotStatus.Baking);
        Find(state, walkInId).Status.Should().Be(OrderItemStatus.Baking);
    }

    [Fact]
    public void Reconcile_WhenNowDoesNotReachBakeEnd_ShouldLeaveItemBaking()
    {
        var timeProvider = new FakeTimeProvider(Start);
        var scheduler = CreateScheduler();
        var itemId = Guid.NewGuid();
        var state = new SchedulerState(
            new[] { CreateQueuedItem(itemId, PriorityLevel.Vip, Start) },
            new[] { CreateFreeSlot(Start) });

        state = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        timeProvider.Advance(BakeTime - TimeSpan.FromMinutes(1));
        state = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        Find(state, itemId).Status.Should().Be(OrderItemStatus.Baking);
        state.Slots[0].Status.Should().Be(OvenSlotStatus.Baking);
    }

    private static SchedulerItemState Find(SchedulerState state, Guid id)
        => state.Items.Single(item => item.OrderItemId == id);

    private static SchedulerItemState CreateQueuedItem(Guid id, PriorityLevel priority, DateTimeOffset enqueuedAt)
        => new(id, priority, SnackType.Cookie, new Money(2.00m, "USD"), OrderItemStatus.Queued, enqueuedAt, null, null, null, null);

    private static SchedulerSlotState CreateFreeSlot(DateTimeOffset availableAt)
        => new(Guid.NewGuid(), Guid.NewGuid(), OvenSlotStatus.Free, availableAt, null, null);

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
                new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = BakeTime }));
        return new KitchenScheduler(provider, new LinearAgingPolicy(provider));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
