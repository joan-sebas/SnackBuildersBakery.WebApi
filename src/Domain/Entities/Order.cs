namespace Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items;
    private readonly List<IDomainEvent> _domainEvents = [];

    public Order(Guid id, PriorityLevel priorityLevel, IEnumerable<OrderItem> items)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Order id cannot be empty.", nameof(id)) : id;

        if (!Enum.IsDefined(priorityLevel))
        {
            throw new InvalidPriorityLevelError(priorityLevel);
        }

        PriorityLevel = priorityLevel;
        _items = items.ToList();
        Status = OrderStatus.AwaitingPayment;

        foreach (var item in _items)
        {
            item.AssignPriority(priorityLevel);
        }
    }

    // Parameterless constructor required for EF Core materialization.
    private Order() { _items = []; }

    public Guid Id { get; private set; }

    public PriorityLevel PriorityLevel { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    internal void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    public bool IsReady => _items.Count > 0 && _items.All(item => item.Status == OrderItemStatus.Ready);

    public Money TotalPrice
    {
        get
        {
            if (_items.Count == 0)
            {
                return new Money(0m, "USD");
            }

            var total = new Money(0m, _items[0].UnitPrice.Currency);

            foreach (var item in _items)
            {
                total += item.UnitPrice;
            }

            return total;
        }
    }

    public void MarkPaid()
    {
        if (Status != OrderStatus.AwaitingPayment)
        {
            throw new OrderAlreadyPaidError(Id);
        }

        Status = OrderStatus.Paid;
    }

    /// <summary>Marks the order paid and raises <see cref="PaymentSucceeded"/>.</summary>
    public void Pay(Guid paymentId, DateTimeOffset now)
    {
        MarkPaid();
        RaiseDomainEvent(new PaymentSucceeded(Id, paymentId, now));
    }
}

public sealed class InvalidPriorityLevelError(PriorityLevel priorityLevel)
    : DomainError($"Priority level is invalid. Value: {priorityLevel}.")
{
    public PriorityLevel PriorityLevel { get; } = priorityLevel;
}
