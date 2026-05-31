namespace Domain;

public abstract class DomainError(string message) : Exception(message);

public sealed class ItemOutOfMenuError(Guid menuItemId)
    : DomainError($"Menu item is not available. MenuItemId: {menuItemId}.")
{
    public Guid MenuItemId { get; } = menuItemId;
}

public sealed class OrderAlreadyPaidError(Guid orderId)
    : DomainError($"Order is already paid. OrderId: {orderId}.")
{
    public Guid OrderId { get; } = orderId;
}

public sealed class InsufficientPaymentError(Money expected, Money received)
    : DomainError($"Payment is insufficient. Expected: {expected.Amount} {expected.Currency}. Received: {received.Amount} {received.Currency}.")
{
    public Money Expected { get; } = expected;

    public Money Received { get; } = received;
}

public sealed class InvalidCapacityError(int capacity)
    : DomainError($"Capacity is invalid. Capacity: {capacity}.")
{
    public int Capacity { get; } = capacity;
}
