using Application;
using Microsoft.Extensions.Options;

namespace Api.Scheduler;

public sealed class KitchenWorkerOptions
{
    public const string SectionName = "KitchenWorker";

    public string ReconcileInterval { get; set; } = "00:00:01";
}

/// <summary>
/// Drives time-based scheduler transitions. The kitchen state advances when a bake finishes,
/// which has no inbound request; this worker reconciles on a fixed cadence so completed bakes
/// turn Ready and freed slots take the next queued item without waiting for the next command.
/// The timer reads the injected <see cref="TimeProvider"/> so tests accelerate it deterministically.
/// </summary>
public sealed class KitchenReconciliationWorker(
    ISchedulerCoordinator coordinator,
    TimeProvider timeProvider,
    IOptions<KitchenWorkerOptions> options,
    ILogger<KitchenReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.Parse(options.Value.ReconcileInterval);
        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await coordinator.ReconcileAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kitchen reconciliation tick failed.");
            }
        }
    }
}
