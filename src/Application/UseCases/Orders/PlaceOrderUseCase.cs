using Domain;

namespace Application;

public sealed class PlaceOrderUseCase(
    IMenuRepository menu,
    IOrderRepository orders,
    IDomainEventDispatcher dispatcher,
    TimeProvider timeProvider)
{
    public async Task<PlaceOrderResult> ExecuteAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var menuItems = await menu.ListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var result = OrderFactory.CreateOrderWithTicket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            request.PriorityLevel,
            request.Lines.Select(l => new OrderFactoryRequestedItem(l.MenuItemId, l.Quantity)),
            menuItems,
            now);

        await orders.AddAsync(result.Order, cancellationToken);

        await dispatcher.DispatchAllAsync(result.Order.DomainEvents, cancellationToken);
        result.Order.ClearDomainEvents();

        return new PlaceOrderResult(result.Ticket);
    }
}
