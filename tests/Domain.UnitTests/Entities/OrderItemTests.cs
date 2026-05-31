using FluentAssertions;

namespace Domain.UnitTests.Entities;

public sealed class OrderItemTests
{
    [Fact]
    public void Constructor_WhenCreatedFromMenuItem_ShouldCaptureImmutablePriceSnapshot()
    {
        var menuItem = new MenuItem(Guid.NewGuid(), "Butter pastry", SnackType.Pastry, new Money(3.50m, "USD"));
        var enqueuedAt = new DateTimeOffset(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

        var orderItem = new OrderItem(Guid.NewGuid(), menuItem, enqueuedAt);
        menuItem.Reprice(new Money(5.00m, "USD"));

        orderItem.UnitPrice.Should().Be(new Money(3.50m, "USD"));
        orderItem.SnackType.Should().Be(SnackType.Pastry);
        orderItem.Status.Should().Be(OrderItemStatus.Queued);
        orderItem.EnqueuedAt.Should().Be(enqueuedAt);
    }

    [Fact]
    public void StartBaking_WhenStatusIsQueued_ShouldMoveToBakingAndCaptureTimestamp()
    {
        var orderItem = CreateOrderItem();
        var startedBakingAt = new DateTimeOffset(2026, 5, 31, 8, 5, 0, TimeSpan.Zero);

        orderItem.StartBaking(startedBakingAt);

        orderItem.Status.Should().Be(OrderItemStatus.Baking);
        orderItem.StartedBakingAt.Should().Be(startedBakingAt);
    }

    [Fact]
    public void MarkReady_WhenStatusIsBaking_ShouldMoveToReadyAndCaptureTimestamp()
    {
        var orderItem = CreateOrderItem();
        orderItem.StartBaking(new DateTimeOffset(2026, 5, 31, 8, 5, 0, TimeSpan.Zero));
        var readyAt = new DateTimeOffset(2026, 5, 31, 8, 10, 0, TimeSpan.Zero);

        orderItem.MarkReady(readyAt);

        orderItem.Status.Should().Be(OrderItemStatus.Ready);
        orderItem.ReadyAt.Should().Be(readyAt);
    }

    [Fact]
    public void Requeue_WhenStatusIsBaking_ShouldThrowDomainError()
    {
        var orderItem = CreateOrderItem();
        orderItem.StartBaking(new DateTimeOffset(2026, 5, 31, 8, 5, 0, TimeSpan.Zero));

        var act = () => orderItem.Requeue(new DateTimeOffset(2026, 5, 31, 8, 6, 0, TimeSpan.Zero));

        act.Should().Throw<InvalidOrderItemTransitionError>();
    }

    [Fact]
    public void MarkReady_WhenStatusIsQueued_ShouldThrowDomainError()
    {
        var orderItem = CreateOrderItem();

        var act = () => orderItem.MarkReady(new DateTimeOffset(2026, 5, 31, 8, 10, 0, TimeSpan.Zero));

        act.Should().Throw<InvalidOrderItemTransitionError>();
    }

    private static OrderItem CreateOrderItem()
    {
        var menuItem = new MenuItem(Guid.NewGuid(), "Seed bread", SnackType.Bread, new Money(4.00m, "USD"));
        return new OrderItem(Guid.NewGuid(), menuItem, new DateTimeOffset(2026, 5, 31, 8, 0, 0, TimeSpan.Zero));
    }
}
