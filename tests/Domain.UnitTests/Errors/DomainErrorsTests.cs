using FluentAssertions;

namespace Domain.UnitTests.Errors;

public sealed class DomainErrorsTests
{
    [Fact]
    public void ItemOutOfMenuError_WhenCreated_ShouldExposeMenuItemId()
    {
        var menuItemId = Guid.NewGuid();

        var error = new ItemOutOfMenuError(menuItemId);

        error.MenuItemId.Should().Be(menuItemId);
        error.Should().BeAssignableTo<DomainError>();
    }

    [Fact]
    public void OrderAlreadyPaidError_WhenCreated_ShouldExposeOrderId()
    {
        var orderId = Guid.NewGuid();

        var error = new OrderAlreadyPaidError(orderId);

        error.OrderId.Should().Be(orderId);
        error.Should().BeAssignableTo<DomainError>();
    }

    [Fact]
    public void InsufficientPaymentError_WhenCreated_ShouldExposeExpectedAndReceivedAmounts()
    {
        var expected = new Money(25.00m, "USD");
        var received = new Money(20.00m, "USD");

        var error = new InsufficientPaymentError(expected, received);

        error.Expected.Should().Be(expected);
        error.Received.Should().Be(received);
        error.Should().BeAssignableTo<DomainError>();
    }

    [Fact]
    public void InvalidCapacityError_WhenCreated_ShouldExposeCapacity()
    {
        const int capacity = 0;

        var error = new InvalidCapacityError(capacity);

        error.Capacity.Should().Be(capacity);
        error.Should().BeAssignableTo<DomainError>();
    }
}
