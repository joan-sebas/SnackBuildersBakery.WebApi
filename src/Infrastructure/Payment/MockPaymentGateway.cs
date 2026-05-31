using Application;
using Domain;
using Microsoft.Extensions.Options;

namespace Infrastructure;

/// <summary>
/// In-process payment simulation. Each method has an independent configurable failure rate
/// so tests and staging can exercise decline paths without touching a real gateway.
/// </summary>
internal sealed class MockPaymentGateway(IOptions<PaymentGatewayOptions> options) : IPaymentGateway
{
    public async Task<PaymentResult> ProcessAsync(
        Money amount,
        PaymentMethod method,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var o = options.Value;

        if (o.SimulatedLatencyMs > 0)
            await Task.Delay(o.SimulatedLatencyMs, cancellationToken);

        var failureRate = method == PaymentMethod.Cash ? o.CashFailureRate : o.CardFailureRate;

        return Random.Shared.NextDouble() < failureRate
            ? PaymentResult.Failure("Payment declined by mock gateway.")
            : PaymentResult.Success($"MOCK-{idempotencyKey:N}");
    }
}
