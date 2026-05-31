namespace Domain;

public sealed class KitchenScheduler(ISchedulerConfigProvider configProvider, IAgingPolicy agingPolicy)
{
    public SchedulerState Reconcile(SchedulerState state, DateTimeOffset now)
    {
        var settings = configProvider.Settings;

        var finishedItemIds = state.Slots
            .Where(slot => slot.BakeFinished(now) && slot.OrderItemId is not null)
            .Select(slot => slot.OrderItemId!.Value)
            .ToHashSet();

        var items = state.Items
            .Select(item => finishedItemIds.Contains(item.OrderItemId) ? item.MarkReady(now) : item)
            .ToList();

        var slots = state.Slots
            .Select(slot => slot.BakeFinished(now) ? slot.IntoTurnover(settings.Turnover) : slot)
            .Select(slot => slot.Release(now))
            .ToList();

        Assign(items, slots, settings, now);

        return new SchedulerState(items, slots);
    }

    public IReadOnlyDictionary<Guid, DateTimeOffset> EstimateReadyTimes(SchedulerState state, DateTimeOffset now)
    {
        var settings = configProvider.Settings;
        var estimates = new Dictionary<Guid, DateTimeOffset>();

        foreach (var item in state.Items)
        {
            if (item.Status == OrderItemStatus.Baking && item.BakingEndsAt is { } endsAt)
            {
                estimates[item.OrderItemId] = endsAt;
            }
        }

        var slotFreeTimes = state.Slots.Select(slot => NextFreeTime(slot, settings.Turnover)).ToList();
        if (slotFreeTimes.Count == 0)
        {
            return estimates;
        }

        var queued = state.Items
            .Where(item => item.Status == OrderItemStatus.Queued)
            .OrderBy(item => SelectionRank.For(agingPolicy, item.PriorityLevel, item.EnqueuedAt, now));

        foreach (var item in queued)
        {
            var slotIndex = EarliestSlot(slotFreeTimes);
            var startAt = Max(slotFreeTimes[slotIndex], now);
            var readyAt = startAt + settings.BakeTimes[item.SnackType];

            estimates[item.OrderItemId] = readyAt;
            slotFreeTimes[slotIndex] = readyAt + settings.Turnover;
        }

        return estimates;
    }

    private static DateTimeOffset NextFreeTime(SchedulerSlotState slot, TimeSpan turnover)
        => slot.Status == OvenSlotStatus.Baking && slot.BakingEndsAt is { } endsAt
            ? endsAt + turnover
            : slot.AvailableAt;

    private static int EarliestSlot(List<DateTimeOffset> slotFreeTimes)
    {
        var earliest = 0;
        for (var index = 1; index < slotFreeTimes.Count; index++)
        {
            if (slotFreeTimes[index] < slotFreeTimes[earliest])
            {
                earliest = index;
            }
        }

        return earliest;
    }

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        => left > right ? left : right;

    private void Assign(
        List<SchedulerItemState> items,
        List<SchedulerSlotState> slots,
        SchedulerSettings settings,
        DateTimeOffset now)
    {
        var freeSlots = new Queue<int>(
            Enumerable.Range(0, slots.Count).Where(index => slots[index].CanStartBaking(now)));

        if (freeSlots.Count == 0)
        {
            return;
        }

        var queuedByPriority = Enumerable.Range(0, items.Count)
            .Where(index => items[index].Status == OrderItemStatus.Queued)
            .OrderBy(index => SelectionRank.For(agingPolicy, items[index].PriorityLevel, items[index].EnqueuedAt, now));

        foreach (var itemIndex in queuedByPriority)
        {
            if (freeSlots.Count == 0)
            {
                break;
            }

            var slotIndex = freeSlots.Dequeue();
            var item = items[itemIndex];
            var bakingEndsAt = now + settings.BakeTimes[item.SnackType];

            items[itemIndex] = item.StartBaking(now, bakingEndsAt, slots[slotIndex].SlotId);
            slots[slotIndex] = slots[slotIndex].StartBaking(item.OrderItemId, bakingEndsAt);
        }
    }
}
