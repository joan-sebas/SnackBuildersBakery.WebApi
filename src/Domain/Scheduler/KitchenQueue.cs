namespace Domain;

public sealed class KitchenQueue
{
    private readonly IAgingPolicy _agingPolicy;
    private readonly List<OrderItem> _items = [];

    public KitchenQueue(IAgingPolicy agingPolicy)
    {
        _agingPolicy = agingPolicy;
    }

    public int Count => _items.Count;

    public IReadOnlyList<OrderItem> Items => _items;

    public void Enqueue(OrderItem item)
    {
        if (item.Status != OrderItemStatus.Queued)
        {
            throw new InvalidOrderItemTransitionError(item.Status, OrderItemStatus.Queued);
        }

        _items.Add(item);
    }

    public bool Remove(OrderItem item)
    {
        return _items.Remove(item);
    }

    public OrderItem? SelectNext(DateTimeOffset now)
    {
        return _items
            .OrderBy(item => SelectionRank.For(_agingPolicy, item.PriorityLevel, item.EnqueuedAt, now))
            .FirstOrDefault();
    }
}
