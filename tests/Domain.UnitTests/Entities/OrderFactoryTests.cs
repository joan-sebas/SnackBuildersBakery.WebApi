using FluentAssertions;

namespace Domain.UnitTests.Entities;

public sealed class OrderFactoryTests
{
    [Fact]
    public void CreateOrderWithTicket_WhenItemsAreValid_ShouldBuildOrderAndTicket()
    {
        var cookie = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"));
        var bread = new MenuItem(Guid.NewGuid(), "Bread", SnackType.Bread, new Money(3.50m, "USD"));
        var menuItems = new[] { cookie, bread };
        var requestedItems = new[]
        {
            new OrderFactoryRequestedItem(cookie.Id, 2),
            new OrderFactoryRequestedItem(bread.Id, 1)
        };

        var result = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PriorityLevel.Vip,
            requestedItems,
            menuItems,
            new DateTimeOffset(2026, 5, 31, 11, 0, 0, TimeSpan.Zero));

        result.Order.Status.Should().Be(OrderStatus.AwaitingPayment);
        result.Order.PriorityLevel.Should().Be(PriorityLevel.Vip);
        result.Order.Items.Should().HaveCount(3);
        result.Order.Items.Should().OnlyContain(item => item.PriorityLevel == PriorityLevel.Vip);
        result.Ticket.TotalPrice.Should().Be(new Money(7.50m, "USD"));
        result.Ticket.EstimatedReadyAt.Should().BeNull();
        result.Ticket.IsEstimateSubjectToPayment.Should().BeTrue();
    }

    [Fact]
    public void CreateOrderWithTicket_WhenMenuPriceChangesAfterCreation_ShouldKeepItemSnapshots()
    {
        var pastry = new MenuItem(Guid.NewGuid(), "Pastry", SnackType.Pastry, new Money(4.00m, "USD"));
        var result = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PriorityLevel.Delivery,
            new[] { new OrderFactoryRequestedItem(pastry.Id, 1) },
            new[] { pastry },
            new DateTimeOffset(2026, 5, 31, 11, 0, 0, TimeSpan.Zero));

        pastry.Reprice(new Money(6.50m, "USD"));

        result.Order.Items[0].UnitPrice.Should().Be(new Money(4.00m, "USD"));
        result.Ticket.TotalPrice.Should().Be(new Money(4.00m, "USD"));
    }

    [Fact]
    public void CreateOrderWithTicket_WhenRequestedItemIsMissingFromMenu_ShouldThrowDomainError()
    {
        var cookie = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"));

        var act = () => OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PriorityLevel.WalkIn,
            new[] { new OrderFactoryRequestedItem(Guid.NewGuid(), 1) },
            new[] { cookie },
            new DateTimeOffset(2026, 5, 31, 11, 0, 0, TimeSpan.Zero));

        act.Should().Throw<ItemOutOfMenuError>();
    }

    [Fact]
    public void CreateOrderWithTicket_WhenRequestedItemIsRemoved_ShouldThrowDomainError()
    {
        var cookie = new MenuItem(Guid.NewGuid(), "Cookie", SnackType.Cookie, new Money(2.00m, "USD"));
        cookie.Remove();

        var act = () => OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PriorityLevel.WalkIn,
            new[] { new OrderFactoryRequestedItem(cookie.Id, 1) },
            new[] { cookie },
            new DateTimeOffset(2026, 5, 31, 11, 0, 0, TimeSpan.Zero));

        act.Should().Throw<ItemOutOfMenuError>();
    }
}
