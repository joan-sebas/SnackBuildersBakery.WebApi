using Domain;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Application.UnitTests.Scheduler;

public sealed class VipRecalculationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Bake = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Turnover = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task EnqueueAsync_WhenVipJoinsAheadOfLowerPriority_ShouldScheduleVipEarlier()
    {
        var coordinator = CreateSingleSlotCoordinator();
        await coordinator.EnqueueAsync(NewItem(PriorityLevel.WalkIn));
        var lower = NewItem(PriorityLevel.WalkIn);
        await coordinator.EnqueueAsync(lower);

        var vip = NewItem(PriorityLevel.Vip);
        await coordinator.EnqueueAsync(vip);

        var estimates = coordinator.EstimateReadyTimes();
        estimates[vip.OrderItemId].Should().BeBefore(estimates[lower.OrderItemId]);
    }

    [Fact]
    public async Task EnqueueAsync_WhenVipArrives_ShouldPushLowerPriorityQueuedEstimateOut()
    {
        var coordinator = CreateSingleSlotCoordinator();
        await coordinator.EnqueueAsync(NewItem(PriorityLevel.WalkIn));
        var lower = NewItem(PriorityLevel.WalkIn);
        await coordinator.EnqueueAsync(lower);
        var beforeVip = coordinator.EstimateReadyTimes()[lower.OrderItemId];

        await coordinator.EnqueueAsync(NewItem(PriorityLevel.Vip));

        var afterVip = coordinator.EstimateReadyTimes()[lower.OrderItemId];
        afterVip.Should().Be(beforeVip + Turnover + Bake);
    }

    [Fact]
    public async Task EnqueueAsync_WhenVipArrives_ShouldNotPreemptOrRecomputeBakingItem()
    {
        var coordinator = CreateSingleSlotCoordinator();
        var baking = NewItem(PriorityLevel.WalkIn);
        await coordinator.EnqueueAsync(baking);
        var bakeEnd = coordinator.EstimateReadyTimes()[baking.OrderItemId];

        await coordinator.EnqueueAsync(NewItem(PriorityLevel.Vip));

        var snapshot = coordinator.GetSnapshot();
        var bakingItem = snapshot.Items.Single(item => item.OrderItemId == baking.OrderItemId);
        bakingItem.Status.Should().Be(OrderItemStatus.Baking);
        bakeEnd.Should().Be(Now + Bake);
        coordinator.EstimateReadyTimes()[baking.OrderItemId].Should().Be(bakeEnd);
    }

    private static EnqueuedItem NewItem(PriorityLevel priority)
        => new(Guid.NewGuid(), priority, SnackType.Cookie, new Money(2.00m, "USD"));

    private static SchedulerCoordinator CreateSingleSlotCoordinator()
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
                OvensCount: 1,
                TraysPerOven: 1,
                Turnover: Turnover,
                new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = Bake }));
        return new SchedulerCoordinator(
            new KitchenScheduler(provider, new LinearAgingPolicy(provider)),
            provider,
            new FakeTimeProvider(Now));
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
