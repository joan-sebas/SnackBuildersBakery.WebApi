using Domain;

namespace Application;

public sealed record TrackOrderResult(
    Guid OrderId,
    OrderStatus OrderStatus,
    IReadOnlyList<OrderItemTracking> Items);

public sealed record OrderItemTracking(
    Guid ItemId,
    SnackType SnackType,
    OrderItemStatus Status,
    DateTimeOffset? EstimatedReadyAt);

public sealed class TrackOrderQuery(IOrderRepository orders, ISchedulerCoordinator scheduler)
{
    public async Task<TrackOrderResult> ExecuteAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order not found. OrderId: {orderId}");

        var schedulerItems = scheduler.GetSnapshot().Items.ToDictionary(i => i.OrderItemId);
        var estimates = scheduler.EstimateReadyTimes();

        var items = order.Items
            .Select(item =>
            {
                var status = schedulerItems.TryGetValue(item.Id, out var schedulerItem)
                    ? schedulerItem.Status
                    : item.Status;
                var readyAt = schedulerItem?.ReadyAt ?? item.ReadyAt;
                var estimated = status == OrderItemStatus.Ready
                    ? readyAt
                    : estimates.TryGetValue(item.Id, out var est) ? est : (DateTimeOffset?)null;

                return new OrderItemTracking(item.Id, item.SnackType, status, estimated);
            })
            .ToList();

        return new TrackOrderResult(order.Id, order.Status, items);
    }
}
