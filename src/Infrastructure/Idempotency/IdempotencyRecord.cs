namespace Infrastructure;

/// <summary>
/// Persisted idempotency entry. The unique index on <see cref="Key"/> is the concurrency
/// guard — a duplicate insert race is resolved by catching the DB violation and reading
/// back the winning row.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid Key { get; set; }
    public string ResultJson { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
