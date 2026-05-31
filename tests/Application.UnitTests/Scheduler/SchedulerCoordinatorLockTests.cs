using Domain;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Application.UnitTests.Scheduler;

public sealed class SchedulerCoordinatorLockTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
    private const int Capacity = 6;

    [Fact]
    public async Task EnqueueAsync_WhenManyCallsRunConcurrently_ShouldNeverExceedCapacityNorLoseItems()
    {
        const int count = 24;
        var coordinator = CreateCoordinator(new FakeTimeProvider(Now));

        var enqueues = Enumerable.Range(0, count)
            .Select(_ => coordinator.EnqueueAsync(NewItem()))
            .ToArray();
        await Task.WhenAll(enqueues);

        var state = coordinator.GetSnapshot();
        var baking = state.Items.Where(item => item.Status == OrderItemStatus.Baking).ToList();
        state.Items.Should().HaveCount(count);
        state.Items.Select(item => item.OrderItemId).Distinct().Should().HaveCount(count);
        baking.Should().HaveCount(Capacity);
        baking.Select(item => item.SlotId).Distinct().Should().HaveCount(Capacity);
        state.Slots.Count(slot => slot.Status == OvenSlotStatus.Baking).Should().Be(Capacity);
    }

    [Fact]
    public async Task GetSnapshot_DuringConcurrentMutations_ShouldStayConsistentAndNeverExceedEnqueuedCount()
    {
        const int count = 24;
        var coordinator = CreateCoordinator(new FakeTimeProvider(Now));

        var enqueues = Enumerable.Range(0, count)
            .Select(_ => coordinator.EnqueueAsync(NewItem()))
            .ToArray();
        var reads = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => coordinator.GetSnapshot()))
            .ToArray();

        await Task.WhenAll(enqueues);
        var snapshots = await Task.WhenAll(reads);

        snapshots.Should().OnlyContain(snapshot => snapshot.Items.Count <= count);
        coordinator.GetSnapshot().Items.Should().HaveCount(count);
    }

    [Fact]
    public async Task EnqueueAsync_WhenSlotsAreFull_ShouldNotPreemptBakingItems()
    {
        var coordinator = CreateCoordinator(new FakeTimeProvider(Now));
        var bakingIds = new List<Guid>();
        for (var i = 0; i < Capacity; i++)
        {
            var item = NewItem(PriorityLevel.WalkIn);
            await coordinator.EnqueueAsync(item);
            bakingIds.Add(item.OrderItemId);
        }

        var late = await coordinator.EnqueueAsync(NewItem(PriorityLevel.Vip));

        late.Items
            .Where(item => bakingIds.Contains(item.OrderItemId))
            .Should().OnlyContain(item => item.Status == OrderItemStatus.Baking);
        late.Items.Single(item => item.PriorityLevel == PriorityLevel.Vip).Status
            .Should().Be(OrderItemStatus.Queued);
    }

    [Fact]
    public async Task EnqueueAsync_WhenRunSingleThreaded_ShouldMatchPureReconcile()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var provider = CreateConfigProvider();
        var scheduler = new KitchenScheduler(provider, new LinearAgingPolicy(provider));
        var coordinator = new SchedulerCoordinator(scheduler, provider, timeProvider);

        var initial = coordinator.GetSnapshot();
        var item = NewItem(PriorityLevel.Vip);

        var actual = await coordinator.EnqueueAsync(item);

        var expectedItems = initial.Items
            .Append(new SchedulerItemState(
                item.OrderItemId, item.PriorityLevel, item.SnackType, item.UnitPrice,
                OrderItemStatus.Queued, Now, null, null, null, null))
            .ToList();
        var expected = scheduler.Reconcile(new SchedulerState(expectedItems, initial.Slots), Now);

        actual.Items.Should().Equal(expected.Items);
        actual.Slots.Should().Equal(expected.Slots);
    }

    [Fact]
    public async Task ReconcileAsync_WhenCalledRepeatedlyAtSameInstant_ShouldBeIdempotent()
    {
        var coordinator = CreateCoordinator(new FakeTimeProvider(Now));
        for (var i = 0; i < Capacity + 2; i++)
        {
            await coordinator.EnqueueAsync(NewItem());
        }

        var once = await coordinator.ReconcileAsync();
        var twice = await coordinator.ReconcileAsync();

        twice.Items.Should().Equal(once.Items);
        twice.Slots.Should().Equal(once.Slots);
    }

    private static EnqueuedItem NewItem(PriorityLevel priority = PriorityLevel.Delivery)
        => new(Guid.NewGuid(), priority, SnackType.Cookie, new Money(2.00m, "USD"));

    private static SchedulerCoordinator CreateCoordinator(TimeProvider timeProvider)
    {
        var provider = CreateConfigProvider();
        return new SchedulerCoordinator(
            new KitchenScheduler(provider, new LinearAgingPolicy(provider)),
            provider,
            timeProvider);
    }

    private static ISchedulerConfigProvider CreateConfigProvider()
        => new TestSchedulerConfigProvider(
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
                new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = TimeSpan.FromMinutes(5) }));

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
