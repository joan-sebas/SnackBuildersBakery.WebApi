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
            .Select(item => new KitchenQueueCandidate(
                item,
                _agingPolicy.CalculateScore(item.PriorityLevel, item.EnqueuedAt, now)))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Item.PriorityLevel)
            .ThenBy(candidate => candidate.Item.EnqueuedAt)
            .Select(candidate => candidate.Item)
            .FirstOrDefault();
    }

    private sealed record KitchenQueueCandidate(OrderItem Item, decimal Score);
}
