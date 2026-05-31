namespace Domain;

// Single source of the kitchen selection rule: highest score first, then higher base
// tier, then earliest enqueued. A higher base tier is the lower PriorityLevel value.
public readonly record struct SelectionRank(decimal Score, PriorityLevel Tier, DateTimeOffset EnqueuedAt)
    : IComparable<SelectionRank>
{
    public static SelectionRank For(
        IAgingPolicy agingPolicy,
        PriorityLevel tier,
        DateTimeOffset enqueuedAt,
        DateTimeOffset now)
        => new(agingPolicy.CalculateScore(tier, enqueuedAt, now), tier, enqueuedAt);

    public int CompareTo(SelectionRank other)
    {
        var byScore = other.Score.CompareTo(Score);
        if (byScore != 0)
        {
            return byScore;
        }

        var byTier = Tier.CompareTo(other.Tier);
        if (byTier != 0)
        {
            return byTier;
        }

        return EnqueuedAt.CompareTo(other.EnqueuedAt);
    }
}
