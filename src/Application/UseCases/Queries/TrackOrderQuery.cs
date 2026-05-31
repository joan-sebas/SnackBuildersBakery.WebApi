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

        var estimates = scheduler.EstimateReadyTimes();

        var items = order.Items
            .Select(item =>
            {
                // Ready items: use the recorded ReadyAt; others: look up in the scheduler projection.
                var estimated = item.Status == OrderItemStatus.Ready
                    ? item.ReadyAt
                    : estimates.TryGetValue(item.Id, out var est) ? est : (DateTimeOffset?)null;

                return new OrderItemTracking(item.Id, item.SnackType, item.Status, estimated);
            })
            .ToList();

        return new TrackOrderResult(order.Id, order.Status, items);
    }
}
