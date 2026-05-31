namespace Domain;

public sealed class OvenSlot
{
    public OvenSlot(Guid id, DateTimeOffset availableAt)
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Oven slot id cannot be empty.", nameof(id)) : id;
        Status = OvenSlotStatus.Free;
        AvailableAt = availableAt;
    }

    public Guid Id { get; }

    public OvenSlotStatus Status { get; private set; }

    public DateTimeOffset AvailableAt { get; private set; }

    public DateTimeOffset? BakingEndsAt { get; private set; }

    public void StartBaking(DateTimeOffset startedBakingAt, DateTimeOffset bakingEndsAt)
    {
        if (Status != OvenSlotStatus.Free)
        {
            throw new InvalidOvenSlotTransitionError(Status, OvenSlotStatus.Baking);
        }

        if (startedBakingAt < AvailableAt)
        {
            throw new InvalidOvenSlotTransitionError(Status, OvenSlotStatus.Baking);
        }

        if (bakingEndsAt < startedBakingAt)
        {
            throw new ArgumentException("Baking end timestamp cannot be earlier than start timestamp.", nameof(bakingEndsAt));
        }

        Status = OvenSlotStatus.Baking;
        BakingEndsAt = bakingEndsAt;
    }

    public void BeginTurnover(TimeSpan baseTurnover)
    {
        if (Status != OvenSlotStatus.Baking)
        {
            throw new InvalidOvenSlotTransitionError(Status, OvenSlotStatus.Turnover);
        }

        if (baseTurnover < TimeSpan.Zero)
        {
            throw new ArgumentException("Turnover duration cannot be negative.", nameof(baseTurnover));
        }

        Status = OvenSlotStatus.Turnover;
        AvailableAt = BakingEndsAt!.Value + baseTurnover;
    }

    public void ExtendTurnover(TimeSpan extraTime)
    {
        if (Status != OvenSlotStatus.Turnover)
        {
            throw new InvalidOvenSlotTransitionError(Status, OvenSlotStatus.Turnover);
        }

        if (extraTime < TimeSpan.Zero)
        {
            throw new ArgumentException("Extra turnover time cannot be negative.", nameof(extraTime));
        }

        AvailableAt += extraTime;
    }

    public void ReleaseIfAvailable(DateTimeOffset now)
    {
        if (Status == OvenSlotStatus.Turnover && now >= AvailableAt)
        {
            Status = OvenSlotStatus.Free;
            BakingEndsAt = null;
        }
    }
}

public enum OvenSlotStatus
{
    Free,
    Baking,
    Turnover
}

public sealed class InvalidOvenSlotTransitionError(OvenSlotStatus from, OvenSlotStatus to)
    : DomainError($"Oven slot transition is invalid. From: {from}. To: {to}.")
{
    public OvenSlotStatus From { get; } = from;

    public OvenSlotStatus To { get; } = to;
}
