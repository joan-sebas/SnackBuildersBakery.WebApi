namespace Domain;

public sealed class KitchenScheduler(ISchedulerConfigProvider configProvider)
{
    public SchedulerState Reconcile(SchedulerState state, DateTimeOffset now)
    {
        var finishedItemIds = state.Slots
            .Where(slot => slot.BakeFinished(now) && slot.OrderItemId is not null)
            .Select(slot => slot.OrderItemId!.Value)
            .ToHashSet();

        var items = state.Items
            .Select(item => finishedItemIds.Contains(item.OrderItemId) ? item.MarkReady(now) : item)
            .ToList();

        var turnover = configProvider.Settings.Turnover;
        var slots = state.Slots
            .Select(slot => slot.BakeFinished(now) ? slot.IntoTurnover(turnover) : slot)
            .Select(slot => slot.Release(now))
            .ToList();

        return new SchedulerState(items, slots);
    }
}
