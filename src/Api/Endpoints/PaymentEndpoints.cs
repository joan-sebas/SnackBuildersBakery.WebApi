using Api.Contracts;
using Api.Idempotency;
using Application;
using Domain;
using System.Text.Json;

namespace Api.Endpoints;

internal static class PaymentEndpoints
{
    internal static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/orders/{id:guid}/payment", async Task<IResult> (
            Guid id,
            HttpContext ctx,
            PayOrderBody body,
            PayOrderUseCase uc,
            IIdempotencyStore idempotency,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Currency) || body.Currency.Length != 3)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["currency"] = ["Currency must be a 3-letter ISO code."] });

            var idempotencyKey = IdempotencyKeyReader.Read(ctx);

            if (idempotencyKey.HasValue)
            {
                var cached = await idempotency.FindAsync(idempotencyKey.Value, ct);
                if (cached.HasValue)
                    return Results.Json(
                        JsonSerializer.Deserialize<PayOrderResponse>(cached.Value.Json),
                        statusCode: cached.Value.StatusCode);
            }

            var result = await uc.ExecuteAsync(
                new PayOrderRequest(id, body.Method, new Money(body.Amount, body.Currency),
                    idempotencyKey ?? Guid.NewGuid()),
                ct);

            var response = new PayOrderResponse(result.IsSuccess, result.FailureReason);
            var statusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status402PaymentRequired;

            if (idempotencyKey.HasValue)
                await idempotency.SaveAsync(idempotencyKey.Value,
                    JsonSerializer.Serialize(response), statusCode, ct);

            return Results.Json(response, statusCode: statusCode);
        }).WithTags("Payments");

        return app;
    }

}
