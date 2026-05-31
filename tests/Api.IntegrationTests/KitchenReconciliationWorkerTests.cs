using Api.Scheduler;
using Application;
using Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Api.IntegrationTests;

public sealed class KitchenReconciliationWorkerTests
{
    [Fact]
    public async Task Worker_WhenIntervalElapses_ReconcilesWithoutAnyRequest()
    {
        var time = new FakeTimeProvider();
        var coordinator = new CountingCoordinator();
        var options = Options.Create(new KitchenWorkerOptions { ReconcileInterval = "00:00:01" });
        var worker = new KitchenReconciliationWorker(
            coordinator, time, options, NullLogger<KitchenReconciliationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        // StartAsync does not wait for ExecuteAsync to register the timer, so advance the
        // clock repeatedly with short real pauses until the worker has ticked at least once.
        for (var attempt = 0; attempt < 50 && !coordinator.FirstReconcile.Task.IsCompleted; attempt++)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(20);
        }

        await coordinator.FirstReconcile.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        coordinator.ReconcileCount.Should().BeGreaterThanOrEqualTo(1);
    }

    private sealed class CountingCoordinator : ISchedulerCoordinator
    {
        private int _reconcileCount;

        public readonly TaskCompletionSource FirstReconcile =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ReconcileCount => Volatile.Read(ref _reconcileCount);

        public SchedulerState GetSnapshot() => new([], []);

        public IReadOnlyDictionary<Guid, DateTimeOffset> EstimateReadyTimes() =>
            new Dictionary<Guid, DateTimeOffset>();

        public Task<SchedulerState> EnqueueAsync(EnqueuedItem item, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetSnapshot());

        public Task<SchedulerState> ReconcileAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _reconcileCount);
            FirstReconcile.TrySetResult();
            return Task.FromResult(GetSnapshot());
        }
    }
}
