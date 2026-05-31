using Application;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Backs the <see cref="IIdempotencyStore"/> port using the same idempotency_records table
/// that payment idempotency uses, so endpoint-level replay is durable across restarts.
/// </summary>
internal sealed class EndpointIdempotencyAdapter(AppDbContext db, TimeProvider timeProvider)
    : IIdempotencyStore
{
    public async Task<(string Json, int StatusCode)?> FindAsync(Guid key, CancellationToken ct = default)
    {
        var record = await db.IdempotencyRecords.FindAsync([key], ct);
        return record is null ? null : (record.ResultJson, record.HttpStatusCode);
    }

    public async Task SaveAsync(Guid key, string json, int statusCode, CancellationToken ct = default)
    {
        var existing = await db.IdempotencyRecords.FindAsync([key], ct);
        if (existing is not null)
        {
            // Payment processing may reserve the same key first; endpoint replay stores the HTTP shape.
            existing.ResultJson = json;
            existing.HttpStatusCode = statusCode;
            await db.SaveChangesAsync(ct);
            return;
        }

        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            ResultJson = json,
            HttpStatusCode = statusCode,
            CreatedAt = timeProvider.GetUtcNow()
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // A concurrent request stored the same key first; the stored record wins.
        }
    }
}
