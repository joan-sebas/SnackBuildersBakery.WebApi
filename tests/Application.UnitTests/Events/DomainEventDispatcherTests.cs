using Domain;
using FluentAssertions;

namespace Application.UnitTests.Events;

public sealed class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WhenHandlerRegistered_ShouldInvokeIt()
    {
        var received = new List<IDomainEvent>();
        var dispatcher = new InMemoryDomainEventDispatcher(
            new[] { new CapturingHandler<OrderPlaced>(received) });
        var evt = new OrderPlaced(Guid.NewGuid(), PriorityLevel.Vip, DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(evt);

        received.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public async Task DispatchAsync_WhenMultipleHandlersRegistered_ShouldInvokeAll()
    {
        var received = new List<IDomainEvent>();
        var dispatcher = new InMemoryDomainEventDispatcher(
            new IDomainEventHandler[]
            {
                new CapturingHandler<OrderPlaced>(received),
                new CapturingHandler<OrderPlaced>(received)
            });
        var evt = new OrderPlaced(Guid.NewGuid(), PriorityLevel.Delivery, DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(evt);

        received.Should().HaveCount(2);
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandlerRegistered_ShouldCompleteWithoutError()
    {
        var dispatcher = new InMemoryDomainEventDispatcher(Array.Empty<IDomainEventHandler>());
        var evt = new OrderPlaced(Guid.NewGuid(), PriorityLevel.WalkIn, DateTimeOffset.UtcNow);

        var act = () => dispatcher.DispatchAsync(evt);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerIsForDifferentEventType_ShouldNotInvokeIt()
    {
        var received = new List<IDomainEvent>();
        var dispatcher = new InMemoryDomainEventDispatcher(
            new[] { new CapturingHandler<PaymentSucceeded>(received) });
        var evt = new OrderPlaced(Guid.NewGuid(), PriorityLevel.Vip, DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(evt);

        received.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAllAsync_ShouldDispatchEachEventInSequence()
    {
        var received = new List<IDomainEvent>();
        var dispatcher = new InMemoryDomainEventDispatcher(
            new IDomainEventHandler[]
            {
                new CapturingHandler<OrderPlaced>(received),
                new CapturingHandler<PaymentSucceeded>(received)
            });
        var events = new IDomainEvent[]
        {
            new OrderPlaced(Guid.NewGuid(), PriorityLevel.Vip, DateTimeOffset.UtcNow),
            new PaymentSucceeded(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow)
        };

        await dispatcher.DispatchAllAsync(events);

        received.Should().HaveCount(2);
    }

    private sealed class CapturingHandler<TEvent>(List<IDomainEvent> sink)
        : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
        {
            sink.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
