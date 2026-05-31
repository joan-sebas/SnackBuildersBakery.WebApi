using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Domain.UnitTests.Scheduler;

public sealed class ReconcileReadyTests
{
    [Fact]
    public void Reconcile_WhenBakeFinished_ShouldMarkItemReadyAndMoveSlotToTurnover()
    {
        var bakingEndsAt = new DateTimeOffset(2026, 5, 31, 15, 10, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(bakingEndsAt);
        var itemId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var ovenId = Guid.NewGuid();
        var state = CreateState(
            CreateBakingItem(itemId, slotId, bakingEndsAt),
            CreateBakingSlot(slotId, ovenId, itemId, bakingEndsAt));
        var scheduler = new KitchenScheduler(CreateConfigProvider(turnover: TimeSpan.FromMinutes(3)));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Items[0].Status.Should().Be(OrderItemStatus.Ready);
        reconciled.Items[0].ReadyAt.Should().Be(timeProvider.GetUtcNow());
        reconciled.Items[0].SlotId.Should().BeNull();
        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Turnover);
        reconciled.Slots[0].AvailableAt.Should().Be(bakingEndsAt + TimeSpan.FromMinutes(3));
        reconciled.Slots[0].OrderItemId.Should().BeNull();
        reconciled.Slots[0].OvenId.Should().Be(ovenId);
    }

    [Fact]
    public void Reconcile_WhenBakeIsStillInProgress_ShouldLeaveItemAndSlotUntouched()
    {
        var now = new DateTimeOffset(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
        var bakingEndsAt = now.AddMinutes(5);
        var timeProvider = new FakeTimeProvider(now);
        var itemId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var ovenId = Guid.NewGuid();
        var item = CreateBakingItem(itemId, slotId, bakingEndsAt);
        var slot = CreateBakingSlot(slotId, ovenId, itemId, bakingEndsAt);
        var state = CreateState(item, slot);
        var scheduler = new KitchenScheduler(CreateConfigProvider(turnover: TimeSpan.FromMinutes(3)));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Items[0].Should().Be(item);
        reconciled.Slots[0].Should().Be(slot);
    }

    [Fact]
    public void Reconcile_WhenTurnoverWindowElapsed_ShouldReleaseSlotToFree()
    {
        var now = new DateTimeOffset(2026, 5, 31, 15, 10, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var ovenId = Guid.NewGuid();
        var slot = new SchedulerSlotState(
            SlotId: Guid.NewGuid(),
            OvenId: ovenId,
            Status: OvenSlotStatus.Turnover,
            AvailableAt: now.AddMinutes(-1),
            OrderItemId: null,
            BakingEndsAt: null);
        var state = new SchedulerState(Array.Empty<SchedulerItemState>(), new[] { slot });
        var scheduler = new KitchenScheduler(CreateConfigProvider(turnover: TimeSpan.FromMinutes(3)));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Free);
        reconciled.Slots[0].AvailableAt.Should().Be(now.AddMinutes(-1));
        reconciled.Slots[0].OvenId.Should().Be(ovenId);
    }

    [Fact]
    public void Reconcile_WhenTurnoverWindowNotElapsed_ShouldKeepSlotInTurnover()
    {
        var now = new DateTimeOffset(2026, 5, 31, 15, 10, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var slot = new SchedulerSlotState(
            SlotId: Guid.NewGuid(),
            OvenId: Guid.NewGuid(),
            Status: OvenSlotStatus.Turnover,
            AvailableAt: now.AddMinutes(2),
            OrderItemId: null,
            BakingEndsAt: null);
        var state = new SchedulerState(Array.Empty<SchedulerItemState>(), new[] { slot });
        var scheduler = new KitchenScheduler(CreateConfigProvider(turnover: TimeSpan.FromMinutes(3)));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Turnover);
    }

    [Fact]
    public void Reconcile_WhenBakeFinished_ShouldNotMutateInputState()
    {
        var bakingEndsAt = new DateTimeOffset(2026, 5, 31, 15, 10, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(bakingEndsAt);
        var itemId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var ovenId = Guid.NewGuid();
        var originalItem = CreateBakingItem(itemId, slotId, bakingEndsAt);
        var originalSlot = CreateBakingSlot(slotId, ovenId, itemId, bakingEndsAt);
        var state = CreateState(originalItem, originalSlot);
        var scheduler = new KitchenScheduler(CreateConfigProvider(turnover: TimeSpan.FromMinutes(3)));

        var reconciled = scheduler.Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Should().NotBeSameAs(state);
        state.Items[0].Should().Be(originalItem);
        state.Slots[0].Should().Be(originalSlot);
    }

    [Fact]
    public void Reconcile_WhenCalledTwiceWithSameNow_ShouldBeIdempotent()
    {
        var bakingEndsAt = new DateTimeOffset(2026, 5, 31, 15, 10, 0, TimeSpan.Zero);
        var itemId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var ovenId = Guid.NewGuid();
        var state = CreateState(
            CreateBakingItem(itemId, slotId, bakingEndsAt),
            CreateBakingSlot(slotId, ovenId, itemId, bakingEndsAt));
        var scheduler = new KitchenScheduler(CreateConfigProvider(turnover: TimeSpan.FromMinutes(3)));

        var once = scheduler.Reconcile(state, bakingEndsAt);
        var twice = scheduler.Reconcile(once, bakingEndsAt);

        twice.Items[0].Should().Be(once.Items[0]);
        twice.Slots[0].Should().Be(once.Slots[0]);
    }

    private static SchedulerState CreateState(SchedulerItemState item, SchedulerSlotState slot)
    {
        return new SchedulerState(new[] { item }, new[] { slot });
    }

    private static SchedulerItemState CreateBakingItem(Guid itemId, Guid slotId, DateTimeOffset bakingEndsAt)
    {
        return new SchedulerItemState(
            OrderItemId: itemId,
            PriorityLevel: PriorityLevel.Delivery,
            SnackType: SnackType.Cookie,
            UnitPrice: new Money(2.00m, "USD"),
            Status: OrderItemStatus.Baking,
            EnqueuedAt: bakingEndsAt.AddMinutes(-10),
            StartedBakingAt: bakingEndsAt.AddMinutes(-5),
            BakingEndsAt: bakingEndsAt,
            ReadyAt: null,
            SlotId: slotId);
    }

    private static SchedulerSlotState CreateBakingSlot(Guid slotId, Guid ovenId, Guid itemId, DateTimeOffset bakingEndsAt)
    {
        return new SchedulerSlotState(
            SlotId: slotId,
            OvenId: ovenId,
            Status: OvenSlotStatus.Baking,
            AvailableAt: bakingEndsAt,
            OrderItemId: itemId,
            BakingEndsAt: bakingEndsAt);
    }

    private static ISchedulerConfigProvider CreateConfigProvider(TimeSpan turnover)
    {
        return new TestSchedulerConfigProvider(
            new SchedulerSettings(
                new Dictionary<PriorityLevel, int>(),
                AgingFactor: 1m,
                Capacity: 4,
                turnover,
                new Dictionary<SnackType, TimeSpan>()));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
