using Api.Auth;
using Api.Contracts;
using Application;

namespace Api.Endpoints;

internal static class KitchenEndpoints
{
    internal static IEndpointRouteBuilder MapKitchenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/kitchen", (KitchenMonitoringQuery query) =>
        {
            var snapshot = query.Execute();
            return Results.Ok(new KitchenSnapshotResponse(
                snapshot.Slots.Select(s => new SlotOccupancyResponse(
                    s.SlotId, s.OvenId, s.Status.ToString(), s.OrderItemId, s.BakingEndsAt)).ToList(),
                snapshot.Queue.Select(q => new QueuedItemResponse(
                    q.OrderItemId, q.SnackType.ToString(), q.PriorityLevel.ToString(), q.EstimatedReadyAt)).ToList()));
        }).RequireManager().WithTags("Kitchen");

        return app;
    }
}
