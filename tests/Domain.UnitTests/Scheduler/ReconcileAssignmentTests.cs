using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Domain.UnitTests.Scheduler;

public sealed class ReconcileAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reconcile_WhenFreeSlotAndQueuedItem_ShouldStartBakingForBakeDuration()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var item = CreateQueuedItem(PriorityLevel.WalkIn, SnackType.Cookie, Now.AddMinutes(-1));
        var slot = CreateFreeSlot(Now);
        var state = new SchedulerState(new[] { item }, new[] { slot });
        var scheduler = CreateScheduler(bakeTime: TimeSpan.FromMinutes(7));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        var bakingEndsAt = Now.AddMinutes(7);
        reconciled.Items[0].Status.Should().Be(OrderItemStatus.Baking);
        reconciled.Items[0].StartedBakingAt.Should().Be(Now);
        reconciled.Items[0].BakingEndsAt.Should().Be(bakingEndsAt);
        reconciled.Items[0].SlotId.Should().Be(slot.SlotId);
        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Baking);
        reconciled.Slots[0].OrderItemId.Should().Be(item.OrderItemId);
        reconciled.Slots[0].BakingEndsAt.Should().Be(bakingEndsAt);
    }

    [Fact]
    public void Reconcile_WhenItemsOutnumberSlots_ShouldAssignBySelectionRule()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var walkIn = CreateQueuedItem(PriorityLevel.WalkIn, SnackType.Cookie, Now);
        var vip = CreateQueuedItem(PriorityLevel.Vip, SnackType.Cookie, Now);
        var state = new SchedulerState(new[] { walkIn, vip }, new[] { CreateFreeSlot(Now) });
        var scheduler = CreateScheduler(bakeTime: TimeSpan.FromMinutes(7));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Items.Single(item => item.OrderItemId == vip.OrderItemId).Status
            .Should().Be(OrderItemStatus.Baking);
        reconciled.Items.Single(item => item.OrderItemId == walkIn.OrderItemId).Status
            .Should().Be(OrderItemStatus.Queued);
    }

    [Fact]
    public void Reconcile_WhenNoSlotIsFree_ShouldLeaveItemQueued()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var item = CreateQueuedItem(PriorityLevel.Vip, SnackType.Cookie, Now);
        var bakingSlot = CreateFreeSlot(Now).StartBaking(Guid.NewGuid(), Now.AddMinutes(5));
        var state = new SchedulerState(new[] { item }, new[] { bakingSlot });
        var scheduler = CreateScheduler(bakeTime: TimeSpan.FromMinutes(7));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Items[0].Status.Should().Be(OrderItemStatus.Queued);
    }

    [Fact]
    public void Reconcile_WhenSlotNotYetAvailable_ShouldNotAssign()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var item = CreateQueuedItem(PriorityLevel.Vip, SnackType.Cookie, Now);
        var slot = CreateFreeSlot(Now.AddMinutes(2));
        var state = new SchedulerState(new[] { item }, new[] { slot });
        var scheduler = CreateScheduler(bakeTime: TimeSpan.FromMinutes(7));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Items[0].Status.Should().Be(OrderItemStatus.Queued);
        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Free);
    }

    [Fact]
    public void Reconcile_WhenCalledTwiceWithSameNow_ShouldBeIdempotent()
    {
        var item = CreateQueuedItem(PriorityLevel.Vip, SnackType.Cookie, Now);
        var slot = CreateFreeSlot(Now);
        var state = new SchedulerState(new[] { item }, new[] { slot });
        var scheduler = CreateScheduler(bakeTime: TimeSpan.FromMinutes(7));

        var once = scheduler.Reconcile(state, Now);
        var twice = scheduler.Reconcile(once, Now);

        twice.Items[0].Should().Be(once.Items[0]);
        twice.Slots[0].Should().Be(once.Slots[0]);
    }

    private static SchedulerItemState CreateQueuedItem(
        PriorityLevel priorityLevel,
        SnackType snackType,
        DateTimeOffset enqueuedAt)
    {
        return new SchedulerItemState(
            OrderItemId: Guid.NewGuid(),
            PriorityLevel: priorityLevel,
            SnackType: snackType,
            UnitPrice: new Money(2.00m, "USD"),
            Status: OrderItemStatus.Queued,
            EnqueuedAt: enqueuedAt,
            StartedBakingAt: null,
            BakingEndsAt: null,
            ReadyAt: null,
            SlotId: null);
    }

    private static SchedulerSlotState CreateFreeSlot(DateTimeOffset availableAt)
    {
        return new SchedulerSlotState(
            SlotId: Guid.NewGuid(),
            OvenId: Guid.NewGuid(),
            Status: OvenSlotStatus.Free,
            AvailableAt: availableAt,
            OrderItemId: null,
            BakingEndsAt: null);
    }

    private static KitchenScheduler CreateScheduler(TimeSpan bakeTime)
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
                Turnover: TimeSpan.FromMinutes(3),
                new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = bakeTime }));
        return new KitchenScheduler(provider, new LinearAgingPolicy(provider));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
