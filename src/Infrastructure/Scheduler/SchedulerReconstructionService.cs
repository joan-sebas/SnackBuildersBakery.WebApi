using Application;
using Domain;

namespace Infrastructure;

/// <summary>
/// Rehydrates the in-memory scheduler from durable order-item rows on startup.
/// Call ReconstructAsync once before the application starts serving requests.
/// </summary>
public sealed class SchedulerReconstructionService(
    IOrderRepository orders,
    ISchedulerCoordinator scheduler)
{
    public async Task ReconstructAsync(CancellationToken cancellationToken = default)
    {
        var items = await orders.GetQueuedAndBakingItemsAsync(cancellationToken);

        foreach (var item in items)
        {
            await scheduler.EnqueueAsync(
                new EnqueuedItem(item.Id, item.PriorityLevel, item.SnackType, item.UnitPrice),
                cancellationToken);
        }
    }
}
