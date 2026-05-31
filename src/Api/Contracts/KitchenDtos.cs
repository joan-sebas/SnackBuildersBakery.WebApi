namespace Api.Contracts;

public sealed record KitchenSnapshotResponse(
    IReadOnlyList<SlotOccupancyResponse> Slots,
    IReadOnlyList<QueuedItemResponse> Queue);

public sealed record SlotOccupancyResponse(
    Guid SlotId,
    Guid OvenId,
    string Status,
    Guid? OrderItemId,
    DateTimeOffset? BakingEndsAt);

public sealed record QueuedItemResponse(
    Guid OrderItemId,
    string SnackType,
    string PriorityLevel,
    DateTimeOffset? EstimatedReadyAt);
