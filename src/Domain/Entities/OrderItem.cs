namespace Domain;

public sealed class OrderItem
{
    public OrderItem(Guid id, MenuItem menuItem, DateTimeOffset enqueuedAt)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Order item id cannot be empty.", nameof(id)) : id;
        MenuItemId = menuItem.Id;
        SnackType = menuItem.SnackType;
        UnitPrice = menuItem.Price;
        Status = OrderItemStatus.Queued;
        EnqueuedAt = enqueuedAt;
    }

    public Guid Id { get; }

    public Guid MenuItemId { get; }

    public SnackType SnackType { get; }

    public Money UnitPrice { get; }

    public OrderItemStatus Status { get; private set; }

    public DateTimeOffset EnqueuedAt { get; private set; }

    public DateTimeOffset? StartedBakingAt { get; private set; }

    public DateTimeOffset? ReadyAt { get; private set; }

    public PriorityLevel PriorityLevel { get; private set; }

    public void AssignPriority(PriorityLevel priorityLevel)
    {
        PriorityLevel = priorityLevel;
    }

    public void StartBaking(DateTimeOffset startedBakingAt)
    {
        EnsureTransitionAllowed(OrderItemStatus.Queued, OrderItemStatus.Baking);
        Status = OrderItemStatus.Baking;
        StartedBakingAt = startedBakingAt;
    }

    public void MarkReady(DateTimeOffset readyAt)
    {
        EnsureTransitionAllowed(OrderItemStatus.Baking, OrderItemStatus.Ready);
        Status = OrderItemStatus.Ready;
        ReadyAt = readyAt;
    }

    public void Requeue(DateTimeOffset enqueuedAt)
    {
        EnsureTransitionAllowed(OrderItemStatus.Queued, OrderItemStatus.Queued);
        EnqueuedAt = enqueuedAt;
        StartedBakingAt = null;
        ReadyAt = null;
    }

    private void EnsureTransitionAllowed(OrderItemStatus expectedCurrentStatus, OrderItemStatus nextStatus)
    {
        if (Status != expectedCurrentStatus)
        {
            throw new InvalidOrderItemTransitionError(Status, nextStatus);
        }
    }
}

public sealed class InvalidOrderItemTransitionError(OrderItemStatus from, OrderItemStatus to)
    : DomainError($"Order item transition is invalid. From: {from}. To: {to}.")
{
    public OrderItemStatus From { get; } = from;

    public OrderItemStatus To { get; } = to;
}
