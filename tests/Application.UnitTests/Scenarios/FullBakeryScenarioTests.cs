using Domain;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Application.UnitTests.Scenarios;

public sealed class FullBakeryScenarioTests
{
    private static readonly DateTimeOffset Start = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Bake = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Turnover = TimeSpan.FromMinutes(2);
    private const int Ovens = 2;
    private const int TraysPerOven = 3;
    private const int Capacity = Ovens * TraysPerOven;

    [Fact]
    public async Task MultipleOrders_WhenDefaultCapacityAndTimeAdvances_ShouldRespectPayFirstPriorityTurnoverAndTracking()
    {
        var time = new FakeTimeProvider(Start);
        var menu = new InMemoryMenuRepository(
            [new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"))]);
        var orders = new InMemoryOrderRepository();
        var dispatcher = new CapturingEventDispatcher();
        var config = new TestSchedulerConfigProvider(new SchedulerSettings(
            new Dictionary<PriorityLevel, int>
            {
                [PriorityLevel.Vip] = 30,
                [PriorityLevel.Delivery] = 20,
                [PriorityLevel.WalkIn] = 10
            },
            AgingFactor: 1m,
            OvensCount: Ovens,
            TraysPerOven: TraysPerOven,
            Turnover: Turnover,
            BakeTimes: new Dictionary<SnackType, TimeSpan> { [SnackType.Cookie] = Bake }));
        var scheduler = new SchedulerCoordinator(
            new KitchenScheduler(config, new LinearAgingPolicy(config)),
            config,
            time);

        var placeOrder = new PlaceOrderUseCase(menu, orders, dispatcher, time);
        var payOrder = new PayOrderUseCase(orders, new SuccessfulPaymentGateway(), scheduler, dispatcher, time);
        var trackOrder = new TrackOrderQuery(orders, scheduler);
        var kitchen = new KitchenMonitoringQuery(scheduler);

        var initialWalkIns = new List<PlacedOrder>();
        for (var i = 0; i < Capacity; i++)
        {
            initialWalkIns.Add(await PlaceAsync(placeOrder, menu, orders, PriorityLevel.WalkIn));
        }

        var queuedWalkIns = new List<PlacedOrder>();
        for (var i = 0; i < Capacity; i++)
        {
            queuedWalkIns.Add(await PlaceAsync(placeOrder, menu, orders, PriorityLevel.WalkIn));
        }

        var vip = await PlaceAsync(placeOrder, menu, orders, PriorityLevel.Vip);

        scheduler.GetSnapshot().Items.Should().BeEmpty("unpaid orders must not enter the kitchen scheduler");

        foreach (var order in initialWalkIns.Concat(queuedWalkIns))
        {
            await PayAsync(payOrder, order.OrderId);
        }

        await PayAsync(payOrder, vip.OrderId);

        var initial = scheduler.GetSnapshot();
        initial.Slots.Should().HaveCount(Capacity);
        initial.Slots.Count(s => s.Status == OvenSlotStatus.Baking).Should().Be(Capacity);
        initial.Items.Where(i => i.Status == OrderItemStatus.Baking)
            .Select(i => i.OrderItemId)
            .Should().BeEquivalentTo(initialWalkIns.Select(o => o.ItemId));
        initial.Items.Should().Contain(i => i.OrderItemId == vip.ItemId && i.Status == OrderItemStatus.Queued);
        initial.Items.Where(i => i.Status == OrderItemStatus.Queued)
            .Select(i => i.OrderItemId)
            .Should().BeEquivalentTo(queuedWalkIns.Select(o => o.ItemId).Append(vip.ItemId));

        var initialKitchen = kitchen.Execute();
        initialKitchen.Queue.Select(q => q.OrderItemId)
            .Should().BeEquivalentTo(queuedWalkIns.Select(o => o.ItemId).Append(vip.ItemId));

        time.Advance(Bake);
        await scheduler.ReconcileAsync();

        var duringTurnover = scheduler.GetSnapshot();
        duringTurnover.Items.Where(i => initialWalkIns.Select(o => o.ItemId).Contains(i.OrderItemId))
            .Should().OnlyContain(i => i.Status == OrderItemStatus.Ready);
        duringTurnover.Slots.Count(s => s.Status == OvenSlotStatus.Turnover).Should().Be(Capacity);
        duringTurnover.Items.Single(i => i.OrderItemId == vip.ItemId).Status.Should().Be(OrderItemStatus.Queued);

        var firstTracking = await trackOrder.ExecuteAsync(initialWalkIns[0].OrderId);
        firstTracking.Items.Single().Status.Should().Be(OrderItemStatus.Ready);
        firstTracking.Items.Single().EstimatedReadyAt.Should().Be(Start + Bake);

        time.Advance(Turnover);
        await scheduler.ReconcileAsync();

        var afterTurnover = scheduler.GetSnapshot();
        var bakingAfterTurnover = afterTurnover.Items.Where(i => i.Status == OrderItemStatus.Baking).ToList();
        bakingAfterTurnover.Should().HaveCount(Capacity);
        afterTurnover.Items.Single(i => i.OrderItemId == vip.ItemId).Status.Should().Be(OrderItemStatus.Baking);
        afterTurnover.Items.Count(i => queuedWalkIns.Select(o => o.ItemId).Contains(i.OrderItemId)
                                      && i.Status == OrderItemStatus.Queued)
            .Should().Be(1);
        afterTurnover.Slots.Should().Contain(s =>
            s.Status == OvenSlotStatus.Baking && s.OrderItemId == vip.ItemId);

        var vipTracking = await trackOrder.ExecuteAsync(vip.OrderId);
        vipTracking.Items.Single().Status.Should().Be(OrderItemStatus.Baking);
        vipTracking.Items.Single().EstimatedReadyAt.Should().Be(Start + Bake + Turnover + Bake);

        time.Advance(Bake + Turnover);
        await scheduler.ReconcileAsync();

        var remainingWalkIn = scheduler.GetSnapshot().Items.Single(i =>
            queuedWalkIns.Select(o => o.ItemId).Contains(i.OrderItemId) && i.Status == OrderItemStatus.Baking);
        kitchen.Execute().Queue.Should().BeEmpty();

        time.Advance(Bake);
        await scheduler.ReconcileAsync();

        var remainingOrder = queuedWalkIns.Single(o => o.ItemId == remainingWalkIn.OrderItemId);
        var remainingTracking = await trackOrder.ExecuteAsync(remainingOrder.OrderId);
        remainingTracking.Items.Single().Status.Should().Be(OrderItemStatus.Ready);
        remainingTracking.Items.Single().EstimatedReadyAt.Should().Be(Start + Bake + Turnover + Bake + Turnover + Bake);

        dispatcher.Dispatched.OfType<OrderPlaced>().Should().HaveCount(Capacity + Capacity + 1);
        dispatcher.Dispatched.OfType<PaymentSucceeded>().Should().HaveCount(Capacity + Capacity + 1);
    }

    private static async Task<PlacedOrder> PlaceAsync(
        PlaceOrderUseCase placeOrder,
        InMemoryMenuRepository menu,
        InMemoryOrderRepository orders,
        PriorityLevel priority)
    {
        var item = (await menu.ListAsync()).Single();
        var result = await placeOrder.ExecuteAsync(
            new PlaceOrderRequest(priority, [new OrderLineRequest(item.Id, 1)]));

        var order = await orders.GetByIdAsync(result.Ticket.OrderId)
            ?? throw new InvalidOperationException("Placed order was not persisted.");

        return new PlacedOrder(order.Id, order.Items.Single().Id);
    }

    private static async Task PayAsync(PayOrderUseCase payOrder, Guid orderId) =>
        await payOrder.ExecuteAsync(new PayOrderRequest(
            orderId,
            PaymentMethod.Cash,
            new Money(2.00m, "USD"),
            Guid.NewGuid()));

    private sealed record PlacedOrder(Guid OrderId, Guid ItemId);

    private sealed class InMemoryMenuRepository(IReadOnlyList<MenuItem> items) : IMenuRepository
    {
        public Task<IReadOnlyList<MenuItem>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(items);

        public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(MenuItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(MenuItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = [];

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrderItem>>(
                _orders.SelectMany(o => o.Items)
                    .Where(i => i.Status is OrderItemStatus.Queued or OrderItemStatus.Baking)
                    .ToList());
    }

    private sealed class SuccessfulPaymentGateway : IPaymentGateway
    {
        public Task<PaymentResult> ProcessAsync(
            Money amount,
            PaymentMethod method,
            Guid idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PaymentResult.Success($"gateway-{idempotencyKey:N}"));
    }

    private sealed class CapturingEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Dispatched.Add(domainEvent);
            return Task.CompletedTask;
        }

        public Task DispatchAllAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }

    private sealed class TestSchedulerConfigProvider(SchedulerSettings settings) : ISchedulerConfigProvider
    {
        public SchedulerSettings Settings { get; } = settings;
    }
}
