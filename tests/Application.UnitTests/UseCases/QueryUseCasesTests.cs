using Domain;
using FluentAssertions;

namespace Application.UnitTests.UseCases;

public sealed class QueryUseCasesTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    // ── TrackOrderQuery ──────────────────────────────────────────────────────

    [Fact]
    public async Task TrackOrderAsync_QueuedItemInScheduler_ShowsProjectedEstimate()
    {
        var order = SampleOrder();
        var itemId = order.Items[0].Id;
        var expectedAt = FixedNow.AddMinutes(5);

        var sut = new TrackOrderQuery(
            new FakeOrderRepository(order),
            new FakeSchedulerCoordinator(
                new SchedulerState([], []),
                estimates: new() { [itemId] = expectedAt }));

        var result = await sut.ExecuteAsync(order.Id);

        result.Items.Single().EstimatedReadyAt.Should().Be(expectedAt);
        result.Items.Single().Status.Should().Be(OrderItemStatus.Queued);
    }

    [Fact]
    public async Task TrackOrderAsync_ReadyItem_ShowsReadyAtAsEstimate()
    {
        var order = SampleOrder();
        var item = order.Items[0];
        item.StartBaking(FixedNow);
        var readyAt = FixedNow.AddMinutes(5);
        item.MarkReady(readyAt);

        var sut = new TrackOrderQuery(
            new FakeOrderRepository(order),
            new FakeSchedulerCoordinator(new SchedulerState([], [])));

        var result = await sut.ExecuteAsync(order.Id);

        result.Items.Single().EstimatedReadyAt.Should().Be(readyAt);
        result.Items.Single().Status.Should().Be(OrderItemStatus.Ready);
    }

    [Fact]
    public async Task TrackOrderAsync_ItemNotInScheduler_ReturnsNullEstimate()
    {
        var order = SampleOrder();

        var sut = new TrackOrderQuery(
            new FakeOrderRepository(order),
            new FakeSchedulerCoordinator(new SchedulerState([], [])));

        var result = await sut.ExecuteAsync(order.Id);

        result.Items.Single().EstimatedReadyAt.Should().BeNull();
        result.OrderStatus.Should().Be(OrderStatus.AwaitingPayment);
    }

    [Fact]
    public async Task TrackOrderAsync_UnknownOrder_ThrowsInvalidOperationException()
    {
        var sut = new TrackOrderQuery(
            new FakeOrderRepository(),
            new FakeSchedulerCoordinator(new SchedulerState([], [])));

        await sut.Invoking(s => s.ExecuteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ── KitchenMonitoringQuery ───────────────────────────────────────────────

    [Fact]
    public void KitchenMonitor_BakingItem_AppearsInSlotNotInQueue()
    {
        var itemId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var ovenId = Guid.NewGuid();
        var endsAt = FixedNow.AddMinutes(5);

        var state = new SchedulerState(
            Items: [BakingItemState(itemId, slotId, endsAt)],
            Slots: [BakingSlotState(slotId, ovenId, endsAt, itemId)]);

        var sut = new KitchenMonitoringQuery(new FakeSchedulerCoordinator(state));
        var snapshot = sut.Execute();

        snapshot.Slots.Should().ContainSingle(s => s.OrderItemId == itemId && s.Status == OvenSlotStatus.Baking);
        snapshot.Queue.Should().BeEmpty();
    }

    [Fact]
    public void KitchenMonitor_QueuedItem_AppearsInQueueWithEstimate()
    {
        var itemId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var ovenId = Guid.NewGuid();
        var estimatedAt = FixedNow.AddMinutes(5);

        var state = new SchedulerState(
            Items: [QueuedItemState(itemId)],
            Slots: [FreeSlotState(slotId, ovenId)]);

        var sut = new KitchenMonitoringQuery(
            new FakeSchedulerCoordinator(state, estimates: new() { [itemId] = estimatedAt }));

        var snapshot = sut.Execute();

        snapshot.Queue.Should().ContainSingle(q => q.OrderItemId == itemId && q.EstimatedReadyAt == estimatedAt);
        snapshot.Slots.Single().OrderItemId.Should().BeNull();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Order SampleOrder()
    {
        var cookie = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(3.50m, "USD"));
        var result = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(), Guid.NewGuid(), PriorityLevel.WalkIn,
            [new OrderFactoryRequestedItem(cookie.Id, 1)],
            [cookie], FixedNow);
        result.Order.ClearDomainEvents();
        return result.Order;
    }

    private static SchedulerItemState BakingItemState(Guid itemId, Guid slotId, DateTimeOffset endsAt) =>
        new(itemId, PriorityLevel.WalkIn, SnackType.Cookie, new Money(3.50m, "USD"),
            OrderItemStatus.Baking, FixedNow, FixedNow, endsAt, ReadyAt: null, slotId);

    private static SchedulerItemState QueuedItemState(Guid itemId) =>
        new(itemId, PriorityLevel.WalkIn, SnackType.Cookie, new Money(3.50m, "USD"),
            OrderItemStatus.Queued, FixedNow,
            StartedBakingAt: null, BakingEndsAt: null, ReadyAt: null, SlotId: null);

    private static SchedulerSlotState BakingSlotState(Guid slotId, Guid ovenId, DateTimeOffset endsAt, Guid itemId) =>
        new(slotId, ovenId, OvenSlotStatus.Baking, AvailableAt: endsAt, itemId, endsAt);

    private static SchedulerSlotState FreeSlotState(Guid slotId, Guid ovenId) =>
        new(slotId, ovenId, OvenSlotStatus.Free, FixedNow, OrderItemId: null, BakingEndsAt: null);

    // ── Fakes ────────────────────────────────────────────────────────────────

    private sealed class FakeOrderRepository(Order? order = null) : IOrderRepository
    {
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(order?.Id == id ? order : null);

        public Task AddAsync(Order o, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Order o, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrderItem>>([]);
    }

    private sealed class FakeSchedulerCoordinator(
        SchedulerState state,
        Dictionary<Guid, DateTimeOffset>? estimates = null) : ISchedulerCoordinator
    {
        public SchedulerState GetSnapshot() => state;

        public IReadOnlyDictionary<Guid, DateTimeOffset> EstimateReadyTimes() =>
            estimates ?? new Dictionary<Guid, DateTimeOffset>();

        public Task<SchedulerState> EnqueueAsync(EnqueuedItem item, CancellationToken ct = default) =>
            Task.FromResult(state);

        public Task<SchedulerState> ReconcileAsync(CancellationToken ct = default) =>
            Task.FromResult(state);
    }
}
