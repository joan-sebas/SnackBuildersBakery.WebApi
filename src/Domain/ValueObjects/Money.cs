namespace Domain;

public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new InvalidMoneyAmountException(amount);
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new MoneyCurrencyMismatchException(currency, currency);
        }

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor)
    {
        if (factor < 0)
        {
            throw new InvalidMoneyAmountException(factor);
        }

        return new Money(money.Amount * factor, money.Currency);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new MoneyCurrencyMismatchException(left.Currency, right.Currency);
        }
    }
}

public sealed class InvalidMoneyAmountException(decimal amount)
    : Exception($"Money amount cannot be negative. Amount: {amount}.");

public sealed class MoneyCurrencyMismatchException(string expectedCurrency, string actualCurrency)
    : Exception($"Money currencies must match. Expected: {expectedCurrency}. Actual: {actualCurrency}.");
