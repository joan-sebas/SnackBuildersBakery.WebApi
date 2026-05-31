using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// EF-backed read/write access to the idempotency_records table.
/// </summary>
internal sealed class IdempotencyStore(AppDbContext db)
{
    public async Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken ct) =>
        await db.IdempotencyRecords.FindAsync([key], ct);

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken ct)
    {
        db.IdempotencyRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }
}
