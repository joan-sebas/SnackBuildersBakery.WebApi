using Domain;

namespace Application;

/// <summary>
/// Thread-safe seam over the pure <see cref="KitchenScheduler"/> policy. Mutations are
/// serialized through a single lock; reads observe the latest committed snapshot lock-free.
/// </summary>
public interface ISchedulerCoordinator
{
    SchedulerState GetSnapshot();

    IReadOnlyDictionary<Guid, DateTimeOffset> EstimateReadyTimes();

    Task<SchedulerState> EnqueueAsync(EnqueuedItem item, CancellationToken cancellationToken = default);

    Task<SchedulerState> ReconcileAsync(CancellationToken cancellationToken = default);
}

public sealed record EnqueuedItem(
    Guid OrderItemId,
    PriorityLevel PriorityLevel,
    SnackType SnackType,
    Money UnitPrice);
