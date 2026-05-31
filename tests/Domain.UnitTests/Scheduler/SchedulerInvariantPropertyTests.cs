using FluentAssertions;
using FsCheck.Xunit;

namespace Domain.UnitTests.Scheduler;

public sealed class SchedulerInvariantPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);

    private static readonly OrderItemStatus[] ItemStatuses =
        [OrderItemStatus.Queued, OrderItemStatus.Baking, OrderItemStatus.Ready];

    private static readonly SnackType[] SnackTypes =
        [SnackType.Cookie, SnackType.Pastry, SnackType.Bread];

    private static readonly PriorityLevel[] Priorities =
        [PriorityLevel.Vip, PriorityLevel.Delivery, PriorityLevel.WalkIn];

    private static readonly OvenSlotStatus[] SlotStatuses =
        [OvenSlotStatus.Free, OvenSlotStatus.Baking, OvenSlotStatus.Turnover];

    [Property(MaxTest = 500)]
    public bool Reconcile_BakingItemIsNeverPreemptedToQueued(int[] itemSeeds, int[] slotSeeds)
    {
        var state = BuildState(itemSeeds, slotSeeds);
        var bakingBefore = state.Items
            .Where(item => item.Status == OrderItemStatus.Baking)
            .Select(item => item.OrderItemId)
            .ToHashSet();

        var after = CreateScheduler().Reconcile(state, Now);

        return bakingBefore.All(id =>
            after.Items.First(item => item.OrderItemId == id).Status != OrderItemStatus.Queued);
    }

    [Property(MaxTest = 500)]
    public bool Reconcile_PreservesItemAndSlotCounts(int[] itemSeeds, int[] slotSeeds)
    {
        var state = BuildState(itemSeeds, slotSeeds);
        var after = CreateScheduler().Reconcile(state, Now);
        return after.Items.Count == state.Items.Count && after.Slots.Count == state.Slots.Count;
    }

    [Property(MaxTest = 500)]
    public bool Reconcile_BakingItemAlwaysCarriesNonNullSlotId(int[] itemSeeds, int[] slotSeeds)
    {
        // A baking item either carried its slot reference from input or was just assigned one;
        // either way the SlotId field must be populated after Reconcile.
        var state = BuildState(itemSeeds, slotSeeds);
        var after = CreateScheduler().Reconcile(state, Now);
        return after.Items
            .Where(item => item.Status == OrderItemStatus.Baking)
            .All(item => item.SlotId.HasValue);
    }

    [Fact]
    public void LinearAgingPolicy_WhenWalkInWaitsLongEnough_ScoreOvertakesVip()
    {
        // No starvation: positive AgingFactor guarantees any item's score eventually exceeds
        // any higher-tier item that just arrived, no matter the tier gap.
        var provider = CreateConfigProvider();
        var policy = new LinearAgingPolicy(provider);
        var settings = provider.Settings;
        var tierGap = settings.TierWeights[PriorityLevel.Vip] - settings.TierWeights[PriorityLevel.WalkIn];
        var overtakeMinutes = (decimal)tierGap / settings.AgingFactor + 1m;
        var waitedAt = Now - TimeSpan.FromMinutes((double)overtakeMinutes);

        var vipScore = policy.CalculateScore(PriorityLevel.Vip, Now, Now);
        var walkInScore = policy.CalculateScore(PriorityLevel.WalkIn, waitedAt, Now);

        walkInScore.Should().BeGreaterThan(vipScore);
    }

    private static SchedulerState BuildState(int[] itemSeeds, int[] slotSeeds)
    {
        var items = (itemSeeds ?? []).Select(BuildItem).ToArray();
        var slots = (slotSeeds is null || slotSeeds.Length == 0 ? [0] : slotSeeds).Select(BuildSlot).ToArray();
        return new SchedulerState(items, slots);
    }

    private static SchedulerItemState BuildItem(int seed)
    {
        var n = Math.Abs(seed);
        var status = ItemStatuses[n % ItemStatuses.Length];
        var snack = SnackTypes[(n / 3) % SnackTypes.Length];
        var priority = Priorities[(n / 9) % Priorities.Length];
        var enqueuedAt = Now.AddMinutes(-(n % 121));
        var bakingEndsAt = Now.AddMinutes((seed % 61) - 30);

        return status switch
        {
            OrderItemStatus.Baking => Item(priority, snack, status, enqueuedAt, Now.AddMinutes(-1), bakingEndsAt, null, Guid.NewGuid()),
            OrderItemStatus.Ready => Item(priority, snack, status, enqueuedAt, Now.AddMinutes(-10), null, Now.AddMinutes(-1), null),
            _ => Item(priority, snack, OrderItemStatus.Queued, enqueuedAt, null, null, null, null)
        };
    }

    private static SchedulerItemState Item(
        PriorityLevel priority, SnackType snack, OrderItemStatus status,
        DateTimeOffset enqueuedAt, DateTimeOffset? startedBakingAt,
        DateTimeOffset? bakingEndsAt, DateTimeOffset? readyAt, Guid? slotId)
        => new(Guid.NewGuid(), priority, snack, new Money(2.00m, "USD"), status, enqueuedAt,
            startedBakingAt, bakingEndsAt, readyAt, slotId);

    private static SchedulerSlotState BuildSlot(int seed)
    {
        var status = SlotStatuses[Math.Abs(seed) % SlotStatuses.Length];
        var availableAt = Now.AddMinutes((seed % 61) - 30);
        return status switch
        {
            OvenSlotStatus.Baking => new SchedulerSlotState(Guid.NewGuid(), Guid.NewGuid(), status, availableAt, Guid.NewGuid(), availableAt),
            OvenSlotStatus.Turnover => new SchedulerSlotState(Guid.NewGuid(), Guid.NewGuid(), status, availableAt, null, null),
            _ => new SchedulerSlotState(Guid.NewGuid(), Guid.NewGuid(), OvenSlotStatus.Free, availableAt, null, null)
        };
    }

    private static KitchenScheduler CreateScheduler()
    {
        var provider = CreateConfigProvider();
        return new KitchenScheduler(provider, new LinearAgingPolicy(provider));
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
                new Dictionary<SnackType, TimeSpan>
                {
                    [SnackType.Cookie] = TimeSpan.FromMinutes(5),
                    [SnackType.Pastry] = TimeSpan.FromMinutes(10),
                    [SnackType.Bread] = TimeSpan.FromMinutes(20)
                }));

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
