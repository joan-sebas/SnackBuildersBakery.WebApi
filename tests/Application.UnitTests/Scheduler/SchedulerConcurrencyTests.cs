using Domain;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Application.UnitTests.Scheduler;

public sealed class SchedulerConcurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);
    private const int Capacity = 6;

    [Fact]
    public async Task ConcurrentEnqueuesAndReconciles_NeverExceedCapacityAndPreserveAllItems()
    {
        const int enqueueCount = 30;
        const int reconcileCount = 20;
        var coordinator = CreateCoordinator();

        var enqueues = Enumerable.Range(0, enqueueCount)
            .Select(_ => coordinator.EnqueueAsync(NewItem()))
            .ToArray();
        var reconciles = Enumerable.Range(0, reconcileCount)
            .Select(_ => coordinator.ReconcileAsync())
            .ToArray();

        await Task.WhenAll(enqueues.Concat(reconciles));

        var state = coordinator.GetSnapshot();
        state.Items.Should().HaveCount(enqueueCount);
        state.Items.Select(item => item.OrderItemId).Distinct().Should().HaveCount(enqueueCount);
        state.Items.Count(item => item.Status == OrderItemStatus.Baking)
            .Should().BeLessThanOrEqualTo(Capacity);
        state.Slots.Count(slot => slot.Status == OvenSlotStatus.Baking)
            .Should().BeLessThanOrEqualTo(Capacity);
    }

    [Fact]
    public async Task EstimateReadyTimes_DuringConcurrentMutations_NeverDeadlocksOrThrows()
    {
        const int enqueueCount = 50;
        var coordinator = CreateCoordinator();

        var enqueues = Enumerable.Range(0, enqueueCount)
            .Select(_ => coordinator.EnqueueAsync(NewItem()))
            .ToArray();
        var reads = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => coordinator.EstimateReadyTimes()))
            .ToArray();

        await Task.WhenAll(enqueues);
        await Task.WhenAll(reads);

        coordinator.GetSnapshot().Items.Should().HaveCount(enqueueCount);
    }

    [Fact]
    public async Task ConcurrentEnqueues_NoBakingItemEverPreemptedToQueued()
    {
        const int rounds = 10;
        var coordinator = CreateCoordinator();
        var bakingIds = new System.Collections.Concurrent.ConcurrentDictionary<Guid, bool>();

        for (var round = 0; round < rounds; round++)
        {
            var before = coordinator.GetSnapshot();
            foreach (var item in before.Items.Where(i => i.Status == OrderItemStatus.Baking))
            {
                bakingIds.TryAdd(item.OrderItemId, true);
            }

            var batch = Enumerable.Range(0, 5)
                .Select(_ => coordinator.EnqueueAsync(NewItem()))
                .ToArray();
            await Task.WhenAll(batch);

            var after = coordinator.GetSnapshot();
            var itemLookup = after.Items.ToDictionary(i => i.OrderItemId);
            foreach (var (id, _) in bakingIds)
            {
                if (itemLookup.TryGetValue(id, out var item))
                {
                    item.Status.Should().NotBe(OrderItemStatus.Queued,
                        "a baking item must never be preempted back to queued");
                }
            }
        }
    }

    private static EnqueuedItem NewItem()
        => new(Guid.NewGuid(), PriorityLevel.Delivery, SnackType.Cookie, new Money(2.00m, "USD"));

    private static SchedulerCoordinator CreateCoordinator()
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
                new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = TimeSpan.FromMinutes(5) }));
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
