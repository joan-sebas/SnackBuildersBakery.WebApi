using FluentAssertions;

namespace Domain.UnitTests.Entities;

public sealed class PaymentTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldStartAsPending()
    {
        var payment = CreatePayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Method.Should().Be(PaymentMethod.Card);
        payment.AmountDue.Should().Be(new Money(10.00m, "USD"));
    }

    [Fact]
    public void Complete_WhenAmountIsSufficient_ShouldMoveToCompleted()
    {
        var payment = CreatePayment();
        var received = new Money(10.00m, "USD");

        payment.Complete(received);

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.AmountReceived.Should().Be(received);
    }

    [Fact]
    public void Complete_WhenAmountIsInsufficient_ShouldThrowDomainError()
    {
        var payment = CreatePayment();

        var act = () => payment.Complete(new Money(8.00m, "USD"));

        act.Should().Throw<InsufficientPaymentError>();
    }

    [Fact]
    public void Fail_WhenStatusIsPending_ShouldMoveToFailed()
    {
        var payment = CreatePayment();

        payment.Fail();

        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void Complete_WhenStatusIsFailed_ShouldThrowDomainError()
    {
        var payment = CreatePayment();
        payment.Fail();

        var act = () => payment.Complete(new Money(10.00m, "USD"));

        act.Should().Throw<InvalidPaymentTransitionError>();
    }

    [Fact]
    public void Fail_WhenStatusIsCompleted_ShouldThrowDomainError()
    {
        var payment = CreatePayment();
        payment.Complete(new Money(10.00m, "USD"));

        var act = () => payment.Fail();

        act.Should().Throw<InvalidPaymentTransitionError>();
    }

    private static Payment CreatePayment()
    {
        return new Payment(Guid.NewGuid(), Guid.NewGuid(), new Money(10.00m, "USD"), PaymentMethod.Card);
    }
}
