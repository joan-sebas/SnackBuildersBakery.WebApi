using Domain;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Application.UnitTests.UseCases;

public sealed class PlaceOrderUseCaseTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid CookieItemId = new("aaaaaaaa-0000-0000-0000-000000000001");

    [Fact]
    public async Task ExecuteAsync_ValidRequest_SavesOrderInAwaitingPayment()
    {
        var repo = new FakeOrderRepository();
        var sut = Build(orderRepo: repo);

        await sut.ExecuteAsync(ValidRequest());

        repo.Saved.Single().Status.Should().Be(OrderStatus.AwaitingPayment);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsTicketWithPositiveTotalSubjectToPayment()
    {
        var sut = Build();

        var result = await sut.ExecuteAsync(ValidRequest());

        result.Ticket.IsEstimateSubjectToPayment.Should().BeTrue();
        result.Ticket.TotalPrice.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_OffMenuItemRequested_ThrowsItemOutOfMenuErrorWithoutSaving()
    {
        var repo = new FakeOrderRepository();
        var sut = Build(menuItems: [], orderRepo: repo);

        await sut.Invoking(s => s.ExecuteAsync(ValidRequest()))
            .Should().ThrowAsync<ItemOutOfMenuError>();

        repo.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_DispatchesOrderPlacedEvent()
    {
        var dispatcher = new FakeEventDispatcher();
        var sut = Build(dispatcher: dispatcher);

        await sut.ExecuteAsync(ValidRequest());

        dispatcher.Dispatched.OfType<OrderPlaced>().Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteAsync_SaveFails_EventsNotDispatched()
    {
        var dispatcher = new FakeEventDispatcher();
        var sut = Build(orderRepo: new ThrowingOrderRepository(), dispatcher: dispatcher);

        await sut.Invoking(s => s.ExecuteAsync(ValidRequest()))
            .Should().ThrowAsync<Exception>();

        dispatcher.Dispatched.Should().BeEmpty();
    }

    private PlaceOrderUseCase Build(
        MenuItem[]? menuItems = null,
        IOrderRepository? orderRepo = null,
        IDomainEventDispatcher? dispatcher = null)
    {
        menuItems ??= [CookieSample()];
        return new PlaceOrderUseCase(
            new FakeMenuRepository(menuItems),
            orderRepo ?? new FakeOrderRepository(),
            dispatcher ?? new FakeEventDispatcher(),
            new FakeTimeProvider(FixedNow));
    }

    private PlaceOrderRequest ValidRequest() =>
        new(PriorityLevel.WalkIn, [new OrderLineRequest(CookieItemId, 2)]);

    private static MenuItem CookieSample() =>
        new(CookieItemId, "Chocolate Chip Cookie", SnackType.Cookie, new Money(3.50m, "USD"));

    private sealed class FakeMenuRepository(IReadOnlyList<MenuItem> items) : IMenuRepository
    {
        public Task<IReadOnlyList<MenuItem>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult(items);

        public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(MenuItem item, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(MenuItem item, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public List<Order> Saved { get; } = [];

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Saved.FirstOrDefault(o => o.Id == id));

        public Task AddAsync(Order order, CancellationToken ct = default)
        {
            Saved.Add(order);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrderItem>>([]);
    }

    private sealed class ThrowingOrderRepository : IOrderRepository
    {
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<Order?>(null);

        public Task AddAsync(Order order, CancellationToken ct = default) =>
            Task.FromException(new InvalidOperationException("simulated persistence failure"));

        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrderItem>>([]);
    }

    private sealed class FakeEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
        {
            Dispatched.Add(domainEvent);
            return Task.CompletedTask;
        }

        public Task DispatchAllAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
        {
            Dispatched.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
