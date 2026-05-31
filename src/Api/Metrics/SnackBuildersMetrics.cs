using Application;
using Domain;
using System.Diagnostics.Metrics;

namespace Api.Metrics;

public sealed class SnackBuildersMetrics : IDisposable
{
    public const string MeterName = "SnackBuilders.Api";
    public const string OrdersPlacedName = "snackbuilders.orders.placed";
    public const string PaymentsProcessedName = "snackbuilders.payments.processed";
    public const string ItemsBakedName = "snackbuilders.items.baked";
    public const string QueueDepthName = "snackbuilders.queue.depth";
    public const string SlotsOccupiedName = "snackbuilders.slots.occupied";

    private readonly Meter _meter;
    private Func<SchedulerState?> _snapshotProvider = () => null;

    public SnackBuildersMetrics(IMeterFactory factory)
    {
        _meter = factory.Create(MeterName);

        OrdersPlaced = _meter.CreateCounter<long>(
            OrdersPlacedName,
            unit: "{order}",
            description: "Number of orders placed.");

        PaymentsProcessed = _meter.CreateCounter<long>(
            PaymentsProcessedName,
            unit: "{payment}",
            description: "Number of payment attempts, tagged by outcome (success|failure).");

        ItemsBaked = _meter.CreateCounter<long>(
            ItemsBakedName,
            unit: "{item}",
            description: "Number of order items that completed baking.");

        QueueDepth = _meter.CreateObservableGauge(
            QueueDepthName,
            ObserveQueueDepth,
            unit: "{item}",
            description: "Current number of queued order items waiting for an oven slot.");

        SlotsOccupied = _meter.CreateObservableGauge(
            SlotsOccupiedName,
            ObserveOccupiedSlots,
            unit: "{slot}",
            description: "Current number of oven slots occupied by baking items.");
    }

    public Counter<long> OrdersPlaced { get; }
    public Counter<long> PaymentsProcessed { get; }
    public Counter<long> ItemsBaked { get; }
    public ObservableGauge<int> QueueDepth { get; }
    public ObservableGauge<int> SlotsOccupied { get; }

    public void RecordOrderPlaced() => OrdersPlaced.Add(1);

    public void RecordPayment(bool success) =>
        PaymentsProcessed.Add(1, new KeyValuePair<string, object?>("outcome", success ? "success" : "failure"));

    internal void ObserveScheduler(Func<SchedulerState> snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    internal void RecordSchedulerTransition(SchedulerState previous, SchedulerState next)
    {
        var bakedDelta = CountItems(next, OrderItemStatus.Ready) - CountItems(previous, OrderItemStatus.Ready);
        if (bakedDelta > 0)
            ItemsBaked.Add(bakedDelta);
    }

    private int ObserveQueueDepth()
    {
        var snapshot = _snapshotProvider();
        return snapshot is null ? 0 : CountItems(snapshot, OrderItemStatus.Queued);
    }

    private int ObserveOccupiedSlots()
    {
        var snapshot = _snapshotProvider();
        return snapshot?.Slots.Count(slot => slot.Status == OvenSlotStatus.Baking) ?? 0;
    }

    private static int CountItems(SchedulerState state, OrderItemStatus status) =>
        state.Items.Count(item => item.Status == status);

    public void Dispose() => _meter.Dispose();
}

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddSnackBuildersMetrics(this IServiceCollection services)
    {
        services.AddMetrics();
        services.AddSingleton<SnackBuildersMetrics>();

        // Decorate the concrete coordinator registered by Infrastructure. Registering the
        // interface again here wins by last-registration, so all consumers get the metered one.
        services.AddSingleton<ISchedulerCoordinator>(sp =>
            new MeteredSchedulerCoordinator(
                sp.GetRequiredService<SchedulerCoordinator>(),
                sp.GetRequiredService<SnackBuildersMetrics>()));

        return services;
    }
}

internal sealed class MeteredSchedulerCoordinator : ISchedulerCoordinator
{
    private readonly ISchedulerCoordinator _inner;
    private readonly SnackBuildersMetrics _metrics;

    public MeteredSchedulerCoordinator(ISchedulerCoordinator inner, SnackBuildersMetrics metrics)
    {
        _inner = inner;
        _metrics = metrics;
        _metrics.ObserveScheduler(_inner.GetSnapshot);
    }

    public SchedulerState GetSnapshot() => _inner.GetSnapshot();

    public IReadOnlyDictionary<Guid, DateTimeOffset> EstimateReadyTimes() => _inner.EstimateReadyTimes();

    public async Task<SchedulerState> EnqueueAsync(EnqueuedItem item, CancellationToken cancellationToken = default)
    {
        var previous = _inner.GetSnapshot();
        var next = await _inner.EnqueueAsync(item, cancellationToken);
        _metrics.RecordSchedulerTransition(previous, next);
        return next;
    }

    public async Task<SchedulerState> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var previous = _inner.GetSnapshot();
        var next = await _inner.ReconcileAsync(cancellationToken);
        _metrics.RecordSchedulerTransition(previous, next);
        return next;
    }
}
