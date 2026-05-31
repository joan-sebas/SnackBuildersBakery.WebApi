using Domain;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Infrastructure.IntegrationTests;

[Collection("Database")]
public sealed class PaymentIdempotencyTests(DatabaseFixture db)
{
    [Fact]
    public async Task SecondCallWithSameKey_ReturnsSameResult_WithoutCallingGatewayAgain()
    {
        await using var ctx = db.CreateContext();
        var gateway = BuildGateway(ctx, failureRate: 0.0);
        var idempotencyKey = Guid.NewGuid();
        var amount = new Money(10m, "USD");

        var first = await gateway.ProcessAsync(amount, PaymentMethod.Card, idempotencyKey);
        var second = await gateway.ProcessAsync(amount, PaymentMethod.Card, idempotencyKey);

        second.IsSuccess.Should().Be(first.IsSuccess);
        second.GatewayReference.Should().Be(first.GatewayReference);
    }

    [Fact]
    public async Task SecondCallWithSameKey_DoesNotInsertDuplicateRecord()
    {
        await using var ctx = db.CreateContext();
        var gateway = BuildGateway(ctx, failureRate: 0.0);
        var idempotencyKey = Guid.NewGuid();
        var amount = new Money(5m, "USD");

        await gateway.ProcessAsync(amount, PaymentMethod.Cash, idempotencyKey);
        await gateway.ProcessAsync(amount, PaymentMethod.Cash, idempotencyKey);

        var count = ctx.IdempotencyRecords.Count(r => r.Key == idempotencyKey);
        count.Should().Be(1);
    }

    [Fact]
    public async Task FailedPaymentResult_IsAlsoPersistedAndReplayed()
    {
        await using var ctx = db.CreateContext();
        var gateway = BuildGateway(ctx, failureRate: 1.0);
        var idempotencyKey = Guid.NewGuid();
        var amount = new Money(3m, "USD");

        var first = await gateway.ProcessAsync(amount, PaymentMethod.Card, idempotencyKey);
        var second = await gateway.ProcessAsync(amount, PaymentMethod.Card, idempotencyKey);

        first.IsSuccess.Should().BeFalse();
        second.IsSuccess.Should().BeFalse();
        second.FailureReason.Should().Be(first.FailureReason);
    }

    private static IdempotentPaymentGateway BuildGateway(AppDbContext ctx, double failureRate)
    {
        var opts = Options.Create(new PaymentGatewayOptions
        {
            CashFailureRate = failureRate,
            CardFailureRate = failureRate,
            SimulatedLatencyMs = 0
        });
        var mock = new MockPaymentGateway(opts);
        var store = new IdempotencyStore(ctx);
        return new IdempotentPaymentGateway(mock, store, TimeProvider.System);
    }
}
