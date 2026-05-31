namespace Application;

/// <summary>
/// Stores and retrieves serialized endpoint results keyed by an idempotency GUID.
/// Allows unsafe endpoints to replay the original response without re-executing the operation.
/// </summary>
public interface IIdempotencyStore
{
    Task<(string Json, int StatusCode)?> FindAsync(Guid key, CancellationToken ct = default);
    Task SaveAsync(Guid key, string json, int statusCode, CancellationToken ct = default);
}
