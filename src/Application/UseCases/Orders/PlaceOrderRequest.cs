using Domain;

namespace Application;

public sealed record PlaceOrderRequest(
    PriorityLevel PriorityLevel,
    IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderLineRequest(Guid MenuItemId, int Quantity);
