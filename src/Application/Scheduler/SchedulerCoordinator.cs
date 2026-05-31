using Domain;

namespace Application;

public sealed class SchedulerCoordinator : ISchedulerCoordinator, IDisposable
{
    private readonly KitchenScheduler _scheduler;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);
    private SchedulerState _state;

    public SchedulerCoordinator(
        KitchenScheduler scheduler,
        ISchedulerConfigProvider configProvider,
        TimeProvider timeProvider)
    {
        _scheduler = scheduler;
        _timeProvider = timeProvider;
        _state = SchedulerStateFactory.CreateInitial(configProvider.Settings, timeProvider.GetUtcNow());
    }

    public SchedulerState GetSnapshot() => Volatile.Read(ref _state);

    public Task<SchedulerState> EnqueueAsync(EnqueuedItem item, CancellationToken cancellationToken = default)
        => MutateAsync((state, now) => Append(state, item, now), cancellationToken);

    public Task<SchedulerState> ReconcileAsync(CancellationToken cancellationToken = default)
        => MutateAsync((state, _) => state, cancellationToken);

    private async Task<SchedulerState> MutateAsync(
        Func<SchedulerState, DateTimeOffset, SchedulerState> change,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var next = _scheduler.Reconcile(change(_state, now), now);
            Volatile.Write(ref _state, next);
            return next;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static SchedulerState Append(SchedulerState state, EnqueuedItem item, DateTimeOffset now)
    {
        var items = new List<SchedulerItemState>(state.Items)
        {
            new(
                item.OrderItemId,
                item.PriorityLevel,
                item.SnackType,
                item.UnitPrice,
                OrderItemStatus.Queued,
                now,
                StartedBakingAt: null,
                BakingEndsAt: null,
                ReadyAt: null,
                SlotId: null)
        };
        return new SchedulerState(items, state.Slots);
    }

    public void Dispose() => _mutationLock.Dispose();
}
