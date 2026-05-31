using Domain;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Application.UnitTests.UseCases;

public sealed class PayOrderUseCaseTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid CookieItemId = new("aaaaaaaa-0000-0000-0000-000000000001");

    [Fact]
    public async Task ExecuteAsync_SuccessfulPayment_MarksOrderPaidAndEnqueuesAllItems()
    {
        var order = CreateOrder();
        var repo = new FakeOrderRepository(order);
        var scheduler = new FakeSchedulerCoordinator();
        var sut = Build(orderRepo: repo, scheduler: scheduler);

        var result = await sut.ExecuteAsync(ValidRequest(order.Id));

        result.IsSuccess.Should().BeTrue();
        repo.Updated.Single().Status.Should().Be(OrderStatus.Paid);
        scheduler.Enqueued.Should().HaveCount(order.Items.Count);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulPayment_DispatchesPaymentSucceededAfterSave()
    {
        var order = CreateOrder();
        var dispatcher = new FakeEventDispatcher();
        var sut = Build(orderRepo: new FakeOrderRepository(order), dispatcher: dispatcher);

        await sut.ExecuteAsync(ValidRequest(order.Id));

        dispatcher.Dispatched.OfType<PaymentSucceeded>().Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulPayment_SaveFails_EventsNotDispatched()
    {
        var order = CreateOrder();
        var dispatcher = new FakeEventDispatcher();
        var repo = new FakeOrderRepository(order) { ThrowOnUpdate = true };
        var sut = Build(orderRepo: repo, dispatcher: dispatcher);

        await sut.Invoking(s => s.ExecuteAsync(ValidRequest(order.Id)))
            .Should().ThrowAsync<Exception>();

        dispatcher.Dispatched.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_GatewayReturnsFailure_OrderRemainsUnpaidWithNoEnqueue()
    {
        var order = CreateOrder();
        var scheduler = new FakeSchedulerCoordinator();
        var sut = Build(
            orderRepo: new FakeOrderRepository(order),
            scheduler: scheduler,
            gateway: new FakePaymentGateway(success: false));

        var result = await sut.ExecuteAsync(ValidRequest(order.Id));

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrWhiteSpace();
        order.Status.Should().Be(OrderStatus.AwaitingPayment);
        scheduler.Enqueued.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyPaidOrder_ThrowsOrderAlreadyPaidErrorWithoutCallingGateway()
    {
        var order = CreateOrder();
        order.MarkPaid();
        var gateway = new FakePaymentGateway(success: true);
        var sut = Build(orderRepo: new FakeOrderRepository(order), gateway: gateway);

        await sut.Invoking(s => s.ExecuteAsync(ValidRequest(order.Id)))
            .Should().ThrowAsync<OrderAlreadyPaidError>();

        gateway.CallCount.Should().Be(0);
    }

    private PayOrderUseCase Build(
        IOrderRepository? orderRepo = null,
        ISchedulerCoordinator? scheduler = null,
        IPaymentGateway? gateway = null,
        IDomainEventDispatcher? dispatcher = null)
    {
        return new PayOrderUseCase(
            orderRepo ?? new FakeOrderRepository(CreateOrder()),
            gateway ?? new FakePaymentGateway(success: true),
            scheduler ?? new FakeSchedulerCoordinator(),
            dispatcher ?? new FakeEventDispatcher(),
            new FakeTimeProvider(FixedNow));
    }

    private static PayOrderRequest ValidRequest(Guid orderId) =>
        new(orderId, PaymentMethod.Card, new Money(7.00m, "USD"), Guid.NewGuid());

    private static Order CreateOrder()
    {
        var cookie = new MenuItem(CookieItemId, "Chocolate Chip Cookie", SnackType.Cookie, new Money(3.50m, "USD"));
        var result = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(), Guid.NewGuid(),
            PriorityLevel.WalkIn,
            [new OrderFactoryRequestedItem(CookieItemId, 2)],
            [cookie],
            FixedNow);
        result.Order.ClearDomainEvents();
        return result.Order;
    }

    private sealed class FakeOrderRepository(Order? order = null) : IOrderRepository
    {
        public bool ThrowOnUpdate { get; init; }
        public List<Order> Updated { get; } = [];

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(order?.Id == id ? order : null);

        public Task AddAsync(Order o, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(Order o, CancellationToken ct = default)
        {
            if (ThrowOnUpdate)
                return Task.FromException(new InvalidOperationException("simulated update failure"));
            Updated.Add(o);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrderItem>>([]);
    }

    private sealed class FakePaymentGateway(bool success, string? failureReason = null) : IPaymentGateway
    {
        public int CallCount { get; private set; }

        public Task<PaymentResult> ProcessAsync(
            Money amount, PaymentMethod method, Guid idempotencyKey, CancellationToken ct = default)
        {
            CallCount++;
            var result = success
                ? PaymentResult.Success("gateway-ref-001")
                : PaymentResult.Failure(failureReason ?? "Payment declined.");
            return Task.FromResult(result);
        }
    }

    private sealed class FakeSchedulerCoordinator : ISchedulerCoordinator
    {
        public List<EnqueuedItem> Enqueued { get; } = [];

        public SchedulerState GetSnapshot() => new([], []);

        public IReadOnlyDictionary<Guid, DateTimeOffset> EstimateReadyTimes() =>
            new Dictionary<Guid, DateTimeOffset>();

        public Task<SchedulerState> EnqueueAsync(EnqueuedItem item, CancellationToken ct = default)
        {
            Enqueued.Add(item);
            return Task.FromResult(new SchedulerState([], []));
        }

        public Task<SchedulerState> ReconcileAsync(CancellationToken ct = default) =>
            Task.FromResult(new SchedulerState([], []));
    }

    private sealed class FakeEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(IDomainEvent e, CancellationToken ct = default)
        {
            Dispatched.Add(e);
            return Task.CompletedTask;
        }

        public Task DispatchAllAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
        {
            Dispatched.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
