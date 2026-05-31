using Application;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default) =>
        await db.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderItem>> GetQueuedAndBakingItemsAsync(
        CancellationToken cancellationToken = default) =>
        await db.Set<OrderItem>()
            .Where(i => i.Status == OrderItemStatus.Queued || i.Status == OrderItemStatus.Baking)
            .ToListAsync(cancellationToken);
}
