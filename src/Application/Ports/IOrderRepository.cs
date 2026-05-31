using Domain;

namespace Application;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all order items currently in <see cref="OrderItemStatus.Queued"/> or
    /// <see cref="OrderItemStatus.Baking"/> status. Used at startup to reconstruct the
    /// in-memory scheduler state from durable order-item data.
    /// </summary>
    Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(CancellationToken cancellationToken = default);
}
