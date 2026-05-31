using Domain;

namespace Application;

public sealed record KitchenSnapshot(
    IReadOnlyList<SlotOccupancy> Slots,
    IReadOnlyList<QueuedItemView> Queue);

public sealed record SlotOccupancy(
    Guid SlotId,
    Guid OvenId,
    OvenSlotStatus Status,
    Guid? OrderItemId,
    DateTimeOffset? BakingEndsAt);

public sealed record QueuedItemView(
    Guid OrderItemId,
    SnackType SnackType,
    PriorityLevel PriorityLevel,
    DateTimeOffset? EstimatedReadyAt);

public sealed class KitchenMonitoringQuery(ISchedulerCoordinator scheduler)
{
    public KitchenSnapshot Execute()
    {
        var state = scheduler.GetSnapshot();
        var estimates = scheduler.EstimateReadyTimes();

        var slots = state.Slots
            .Select(s => new SlotOccupancy(s.SlotId, s.OvenId, s.Status, s.OrderItemId, s.BakingEndsAt))
            .ToList();

        var queue = state.Items
            .Where(i => i.Status == OrderItemStatus.Queued)
            .Select(i =>
            {
                var estimated = estimates.TryGetValue(i.OrderItemId, out var est) ? est : (DateTimeOffset?)null;
                return new QueuedItemView(i.OrderItemId, i.SnackType, i.PriorityLevel, estimated);
            })
            .ToList();

        return new KitchenSnapshot(slots, queue);
    }
}
