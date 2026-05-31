using System.Text.Json;
using Application;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Decorator that persists each payment result keyed by the idempotency GUID.
/// A second call with the same key returns the first result without calling the gateway again.
/// A concurrent duplicate insert is resolved by catching the PK violation and reading the winner's row.
/// </summary>
internal sealed class IdempotentPaymentGateway(
    MockPaymentGateway inner,
    IdempotencyStore store,
    TimeProvider timeProvider) : IPaymentGateway
{
    public async Task<PaymentResult> ProcessAsync(
        Money amount,
        PaymentMethod method,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var existing = await store.FindAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
            return JsonSerializer.Deserialize<PaymentResult>(existing.ResultJson)!;

        var result = await inner.ProcessAsync(amount, method, idempotencyKey, cancellationToken);

        var record = new IdempotencyRecord
        {
            Key = idempotencyKey,
            ResultJson = JsonSerializer.Serialize(result),
            HttpStatusCode = result.IsSuccess ? 200 : 402,
            CreatedAt = timeProvider.GetUtcNow()
        };

        try
        {
            await store.SaveAsync(record, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Lost the concurrent insert race — return the winning row instead.
            var winner = await store.FindAsync(idempotencyKey, cancellationToken);
            return JsonSerializer.Deserialize<PaymentResult>(winner!.ResultJson)!;
        }

        return result;
    }

    // Postgres error code 23505 = unique_violation.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
}
