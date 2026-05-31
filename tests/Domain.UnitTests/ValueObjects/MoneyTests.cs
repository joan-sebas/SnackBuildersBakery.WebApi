using FluentAssertions;

namespace Domain.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_WhenAmountIsNegative_ShouldThrowDomainError()
    {
        var act = () => new Money(-0.01m, "USD");

        act.Should().Throw<InvalidMoneyAmountException>();
    }

    [Fact]
    public void Equals_WhenValuesMatch_ShouldReturnTrue()
    {
        var left = new Money(10.50m, "USD");
        var right = new Money(10.50m, "USD");

        left.Should().Be(right);
    }

    [Fact]
    public void Add_WhenCurrencyMatches_ShouldReturnSummedMoney()
    {
        var left = new Money(0.10m, "USD");
        var right = new Money(0.20m, "USD");

        var result = left + right;

        result.Amount.Should().Be(0.30m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_WhenCurrencyDiffers_ShouldThrowDomainError()
    {
        var left = new Money(1.00m, "USD");
        var right = new Money(1.00m, "EUR");

        var act = () => left + right;

        act.Should().Throw<MoneyCurrencyMismatchException>();
    }

    [Fact]
    public void Multiply_WhenFactorIsNonNegative_ShouldPreserveDecimalPrecision()
    {
        var money = new Money(1.23m, "USD");

        var result = money * 3;

        result.Amount.Should().Be(3.69m);
        result.Currency.Should().Be("USD");
    }
}
