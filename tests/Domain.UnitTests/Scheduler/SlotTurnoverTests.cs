using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Domain.UnitTests.Scheduler;

public sealed class SlotTurnoverTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Turnover = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan BakeTime = TimeSpan.FromMinutes(5);

    [Fact]
    public void Reconcile_WhenBakeFinishesWithItemWaiting_ShouldHoldSlotForTheTurnoverWindow()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var bakingId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var baking = CreateBakingItem(bakingId, slotId, bakingEndsAt: Now);
        var slot = CreateBakingSlot(slotId, bakingId, bakingEndsAt: Now);
        var waiting = CreateQueuedItem(SnackType.Cookie, Now.AddMinutes(-1));
        var state = new SchedulerState(new[] { baking, waiting }, new[] { slot });

        var reconciled = CreateScheduler().Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Turnover);
        reconciled.Slots[0].AvailableAt.Should().Be(Now + Turnover);
        reconciled.Items.Single(item => item.OrderItemId == waiting.OrderItemId).Status
            .Should().Be(OrderItemStatus.Queued);
    }

    [Fact]
    public void Reconcile_WhenTurnoverWindowNotYetElapsed_ShouldNotAssignWaitingItem()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var slot = CreateTurnoverSlot(availableAt: Now.AddMinutes(1));
        var waiting = CreateQueuedItem(SnackType.Cookie, Now);
        var state = new SchedulerState(new[] { waiting }, new[] { slot });

        var reconciled = CreateScheduler().Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Turnover);
        reconciled.Items[0].Status.Should().Be(OrderItemStatus.Queued);
    }

    [Fact]
    public void Reconcile_WhenTurnoverWindowElapsed_ShouldReleaseAndAssignWaitingItem()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var slot = CreateTurnoverSlot(availableAt: Now);
        var waiting = CreateQueuedItem(SnackType.Cookie, Now);
        var state = new SchedulerState(new[] { waiting }, new[] { slot });

        var reconciled = CreateScheduler().Reconcile(state, timeProvider.GetUtcNow());

        reconciled.Slots[0].Status.Should().Be(OvenSlotStatus.Baking);
        reconciled.Slots[0].OrderItemId.Should().Be(waiting.OrderItemId);
        reconciled.Items[0].Status.Should().Be(OrderItemStatus.Baking);
    }

    [Fact]
    public void IntoTurnover_ShouldPushAvailableAtForwardNeverBackward()
    {
        var bakingEndsAt = Now;
        var slot = CreateBakingSlot(Guid.NewGuid(), Guid.NewGuid(), bakingEndsAt);

        var inTurnover = slot.IntoTurnover(Turnover);

        inTurnover.AvailableAt.Should().Be(bakingEndsAt + Turnover);
        inTurnover.AvailableAt.Should().BeAfter(slot.AvailableAt);
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
            StartedBakingAt: bakingEndsAt - BakeTime,
            BakingEndsAt: bakingEndsAt,
            ReadyAt: null,
            SlotId: slotId);
    }

    private static SchedulerItemState CreateQueuedItem(SnackType snackType, DateTimeOffset enqueuedAt)
    {
        return new SchedulerItemState(
            OrderItemId: Guid.NewGuid(),
            PriorityLevel: PriorityLevel.Vip,
            SnackType: snackType,
            UnitPrice: new Money(2.00m, "USD"),
            Status: OrderItemStatus.Queued,
            EnqueuedAt: enqueuedAt,
            StartedBakingAt: null,
            BakingEndsAt: null,
            ReadyAt: null,
            SlotId: null);
    }

    private static SchedulerSlotState CreateBakingSlot(Guid slotId, Guid itemId, DateTimeOffset bakingEndsAt)
    {
        return new SchedulerSlotState(
            SlotId: slotId,
            OvenId: Guid.NewGuid(),
            Status: OvenSlotStatus.Baking,
            AvailableAt: bakingEndsAt,
            OrderItemId: itemId,
            BakingEndsAt: bakingEndsAt);
    }

    private static SchedulerSlotState CreateTurnoverSlot(DateTimeOffset availableAt)
    {
        return new SchedulerSlotState(
            SlotId: Guid.NewGuid(),
            OvenId: Guid.NewGuid(),
            Status: OvenSlotStatus.Turnover,
            AvailableAt: availableAt,
            OrderItemId: null,
            BakingEndsAt: null);
    }

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
