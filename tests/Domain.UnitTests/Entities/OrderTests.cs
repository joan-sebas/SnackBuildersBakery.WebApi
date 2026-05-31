using FluentAssertions;

namespace Domain.UnitTests.Entities;

public sealed class OrderTests
{
    [Fact]
    public void Constructor_WhenPriorityLevelIsInvalid_ShouldThrowDomainError()
    {
        var items = new[] { CreateOrderItem() };

        var act = () => new Order(Guid.NewGuid(), (PriorityLevel)999, items);

        act.Should().Throw<InvalidPriorityLevelError>();
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldPropagatePriorityToItems()
    {
        var first = CreateOrderItem();
        var second = CreateOrderItem();

        var order = new Order(Guid.NewGuid(), PriorityLevel.Delivery, new[] { first, second });

        order.PriorityLevel.Should().Be(PriorityLevel.Delivery);
        first.PriorityLevel.Should().Be(PriorityLevel.Delivery);
        second.PriorityLevel.Should().Be(PriorityLevel.Delivery);
    }

    [Fact]
    public void MarkPaid_WhenStatusIsAwaitingPayment_ShouldMoveToPaid()
    {
        var order = new Order(Guid.NewGuid(), PriorityLevel.Vip, new[] { CreateOrderItem() });

        order.MarkPaid();

        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void MarkPaid_WhenStatusIsPaid_ShouldThrowDomainError()
    {
        var order = new Order(Guid.NewGuid(), PriorityLevel.Vip, new[] { CreateOrderItem() });
        order.MarkPaid();

        var act = () => order.MarkPaid();

        act.Should().Throw<OrderAlreadyPaidError>();
    }

    [Fact]
    public void IsReady_WhenAnyItemIsNotReady_ShouldBeFalse()
    {
        var first = CreateOrderItem();
        var second = CreateOrderItem();

        var order = new Order(Guid.NewGuid(), PriorityLevel.WalkIn, new[] { first, second });

        first.StartBaking(new DateTimeOffset(2026, 5, 31, 9, 1, 0, TimeSpan.Zero));
        first.MarkReady(new DateTimeOffset(2026, 5, 31, 9, 5, 0, TimeSpan.Zero));

        order.IsReady.Should().BeFalse();
    }

    [Fact]
    public void IsReady_WhenAllItemsAreReady_ShouldBeTrue()
    {
        var first = CreateOrderItem();
        var second = CreateOrderItem();
        var order = new Order(Guid.NewGuid(), PriorityLevel.WalkIn, new[] { first, second });

        first.StartBaking(new DateTimeOffset(2026, 5, 31, 9, 1, 0, TimeSpan.Zero));
        first.MarkReady(new DateTimeOffset(2026, 5, 31, 9, 5, 0, TimeSpan.Zero));
        second.StartBaking(new DateTimeOffset(2026, 5, 31, 9, 2, 0, TimeSpan.Zero));
        second.MarkReady(new DateTimeOffset(2026, 5, 31, 9, 6, 0, TimeSpan.Zero));

        order.IsReady.Should().BeTrue();
    }

    [Fact]
    public void TotalPrice_WhenItemsArePresent_ShouldBeCalculatedFromSnapshots()
    {
        var menuItemOne = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"));
        var menuItemTwo = new MenuItem(Guid.NewGuid(), "Bread", SnackType.Bread, new Money(3.50m, "USD"));
        var first = new OrderItem(Guid.NewGuid(), menuItemOne, new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero));
        var second = new OrderItem(Guid.NewGuid(), menuItemTwo, new DateTimeOffset(2026, 5, 31, 9, 1, 0, TimeSpan.Zero));

        var order = new Order(Guid.NewGuid(), PriorityLevel.WalkIn, new[] { first, second });

        order.TotalPrice.Should().Be(new Money(5.50m, "USD"));
    }

    private static OrderItem CreateOrderItem()
    {
        var menuItem = new MenuItem(Guid.NewGuid(), "Pastry", SnackType.Pastry, new Money(4.00m, "USD"));
        return new OrderItem(Guid.NewGuid(), menuItem, new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero));
    }
}
