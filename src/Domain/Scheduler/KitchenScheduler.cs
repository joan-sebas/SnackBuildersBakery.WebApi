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
