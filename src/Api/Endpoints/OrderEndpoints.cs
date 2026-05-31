using Api.Contracts;
using Api.Idempotency;
using Api.Metrics;
using Application;
using System.Text.Json;

namespace Api.Endpoints;

internal static class OrderEndpoints
{
    internal static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/orders").WithTags("Orders");

        group.MapPost("/", async Task<IResult> (
            HttpContext ctx,
            PlaceOrderBody body,
            PlaceOrderUseCase uc,
            IIdempotencyStore idempotency,
            SnackBuildersMetrics metrics,
            CancellationToken ct) =>
        {
            var idempotencyKey = IdempotencyKeyReader.Read(ctx);

            if (idempotencyKey.HasValue)
            {
                var cached = await idempotency.FindAsync(idempotencyKey.Value, ct);
                if (cached.HasValue)
                    return Results.Json(JsonSerializer.Deserialize<TicketResponse>(cached.Value.Json),
                        statusCode: cached.Value.StatusCode);
            }

            if (body.Lines is null || body.Lines.Count == 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["lines"] = ["At least one order line is required."] });

            var result = await uc.ExecuteAsync(
                new PlaceOrderRequest(body.PriorityLevel,
                    body.Lines.Select(l => new OrderLineRequest(l.MenuItemId, l.Quantity)).ToList()),
                ct);

            var response = ToTicketResponse(result.Ticket);

            if (idempotencyKey.HasValue)
                await idempotency.SaveAsync(idempotencyKey.Value, JsonSerializer.Serialize(response),
                    StatusCodes.Status201Created, ct);

            metrics.RecordOrderPlaced();
            return Results.Created($"/v1/orders/{response.OrderId}", response);
        });

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, TrackOrderQuery query, CancellationToken ct) =>
        {
            try
            {
                var result = await query.ExecuteAsync(id, ct);
                return Results.Ok(new TrackOrderResponse(
                    result.OrderId,
                    result.OrderStatus.ToString(),
                    result.Items.Select(i => new OrderItemTrackingResponse(
                        i.ItemId, i.SnackType.ToString(), i.Status.ToString(), i.EstimatedReadyAt)).ToList()));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found",
                    detail: ex.Message);
            }
        });

        return app;
    }

    private static TicketResponse ToTicketResponse(Domain.Ticket ticket) =>
        new(ticket.Id, ticket.OrderId, ticket.TotalPrice.Amount, ticket.TotalPrice.Currency,
            ticket.EstimatedReadyAt, ticket.IsEstimateSubjectToPayment);
}
