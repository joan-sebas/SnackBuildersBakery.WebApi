namespace Domain;

public static class OrderFactory
{
    public static OrderFactoryResult CreateOrderWithTicket(
        Guid orderId,
        Guid ticketId,
        PriorityLevel priorityLevel,
        IEnumerable<OrderFactoryRequestedItem> requestedItems,
        IEnumerable<MenuItem> menuItems,
        DateTimeOffset enqueuedAt)
    {
        var menuById = menuItems.ToDictionary(item => item.Id);
        var orderItems = new List<OrderItem>();

        foreach (var requestedItem in requestedItems)
        {
            if (requestedItem.Quantity <= 0)
            {
                throw new ArgumentException("Requested item quantity must be greater than zero.", nameof(requestedItems));
            }

            if (!menuById.TryGetValue(requestedItem.MenuItemId, out var menuItem) || menuItem.IsRemoved)
            {
                throw new ItemOutOfMenuError(requestedItem.MenuItemId);
            }

            for (var index = 0; index < requestedItem.Quantity; index++)
            {
                orderItems.Add(new OrderItem(Guid.NewGuid(), menuItem, enqueuedAt));
            }
        }

        var order = new Order(orderId, priorityLevel, orderItems);
        var ticket = new Ticket(ticketId, order.Id, order.TotalPrice, estimatedReadyAt: null, isEstimateSubjectToPayment: true);

        return new OrderFactoryResult(order, ticket);
    }
}

public readonly record struct OrderFactoryRequestedItem(Guid MenuItemId, int Quantity);

public sealed record OrderFactoryResult(Order Order, Ticket Ticket);
