using Domain;

namespace Api.Contracts;

public sealed record PlaceOrderBody(
    PriorityLevel PriorityLevel,
    IReadOnlyList<OrderLineBody> Lines);

public sealed record OrderLineBody(Guid MenuItemId, int Quantity);

public sealed record TicketResponse(
    Guid TicketId,
    Guid OrderId,
    decimal TotalPrice,
    string Currency,
    DateTimeOffset? EstimatedReadyAt,
    bool IsEstimateSubjectToPayment);

public sealed record TrackOrderResponse(
    Guid OrderId,
    string OrderStatus,
    IReadOnlyList<OrderItemTrackingResponse> Items);

public sealed record OrderItemTrackingResponse(
    Guid ItemId,
    string SnackType,
    string Status,
    DateTimeOffset? EstimatedReadyAt);
