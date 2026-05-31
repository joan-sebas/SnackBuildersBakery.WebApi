namespace Domain;

public sealed class Payment
{
    public Payment(Guid id, Guid orderId, Money amountDue, PaymentMethod method)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Payment id cannot be empty.", nameof(id)) : id;
        OrderId = orderId == Guid.Empty ? throw new ArgumentException("Order id cannot be empty.", nameof(orderId)) : orderId;
        AmountDue = amountDue;
        Method = method;
        Status = PaymentStatus.Pending;
    }

    public Guid Id { get; }

    public Guid OrderId { get; }

    public Money AmountDue { get; }

    public Money? AmountReceived { get; private set; }

    public PaymentMethod Method { get; }

    public PaymentStatus Status { get; private set; }

    public void Complete(Money amountReceived)
    {
        EnsureTransitionAllowed(PaymentStatus.Pending, PaymentStatus.Completed);

        if (amountReceived.Amount < AmountDue.Amount)
        {
            throw new InsufficientPaymentError(AmountDue, amountReceived);
        }

        if (!string.Equals(amountReceived.Currency, AmountDue.Currency, StringComparison.Ordinal))
        {
            throw new MoneyCurrencyMismatchException(AmountDue.Currency, amountReceived.Currency);
        }

        AmountReceived = amountReceived;
        Status = PaymentStatus.Completed;
    }

    public void Fail()
    {
        EnsureTransitionAllowed(PaymentStatus.Pending, PaymentStatus.Failed);
        Status = PaymentStatus.Failed;
    }

    private void EnsureTransitionAllowed(PaymentStatus expectedCurrentStatus, PaymentStatus nextStatus)
    {
        if (Status != expectedCurrentStatus)
        {
            throw new InvalidPaymentTransitionError(Status, nextStatus);
        }
    }
}

public sealed class InvalidPaymentTransitionError(PaymentStatus from, PaymentStatus to)
    : DomainError($"Payment transition is invalid. From: {from}. To: {to}.")
{
    public PaymentStatus From { get; } = from;

    public PaymentStatus To { get; } = to;
}
