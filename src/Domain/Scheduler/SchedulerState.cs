namespace Domain;

public sealed record SchedulerState(
    IReadOnlyList<SchedulerItemState> Items,
    IReadOnlyList<SchedulerSlotState> Slots);

public sealed record SchedulerItemState(
    Guid OrderItemId,
    PriorityLevel PriorityLevel,
    SnackType SnackType,
    Money UnitPrice,
    OrderItemStatus Status,
    DateTimeOffset EnqueuedAt,
    DateTimeOffset? StartedBakingAt,
    DateTimeOffset? BakingEndsAt,
    DateTimeOffset? ReadyAt,
    Guid? SlotId)
{
    public SchedulerItemState MarkReady(DateTimeOffset now) => this with
    {
        Status = OrderItemStatus.Ready,
        ReadyAt = now,
        SlotId = null
    };
}

public sealed record SchedulerSlotState(
    Guid SlotId,
    Guid OvenId,
    OvenSlotStatus Status,
    DateTimeOffset AvailableAt,
    Guid? OrderItemId,
    DateTimeOffset? BakingEndsAt)
{
    public bool BakeFinished(DateTimeOffset now)
        => Status == OvenSlotStatus.Baking && BakingEndsAt is { } endsAt && endsAt <= now;

    public SchedulerSlotState IntoTurnover(TimeSpan turnover)
    {
        if (Status != OvenSlotStatus.Baking)
        {
            throw new InvalidOvenSlotTransitionError(Status, OvenSlotStatus.Turnover);
        }

        return this with
        {
            Status = OvenSlotStatus.Turnover,
            AvailableAt = BakingEndsAt!.Value + turnover,
            OrderItemId = null,
            BakingEndsAt = null
        };
    }

    public SchedulerSlotState Release(DateTimeOffset now)
        => Status == OvenSlotStatus.Turnover && AvailableAt <= now
            ? this with { Status = OvenSlotStatus.Free }
            : this;
}
