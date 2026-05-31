namespace Domain;

public sealed class Ticket
{
    public Ticket(Guid id, Guid orderId, Money totalPrice, DateTimeOffset? estimatedReadyAt, bool isEstimateSubjectToPayment)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Ticket id cannot be empty.", nameof(id)) : id;
        OrderId = orderId == Guid.Empty ? throw new ArgumentException("Order id cannot be empty.", nameof(orderId)) : orderId;
        TotalPrice = totalPrice;
        EstimatedReadyAt = estimatedReadyAt;
        IsEstimateSubjectToPayment = isEstimateSubjectToPayment;
    }

    public Guid Id { get; }

    public Guid OrderId { get; }

    public Money TotalPrice { get; }

    public DateTimeOffset? EstimatedReadyAt { get; }

    public bool IsEstimateSubjectToPayment { get; }
}
