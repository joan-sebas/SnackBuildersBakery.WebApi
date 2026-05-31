using Api.Auth;
using Api.Contracts;
using Application;
using Domain;

namespace Api.Endpoints;

internal static class MenuEndpoints
{
    internal static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/menu").WithTags("Menu");

        group.MapGet("/", async Task<IResult> (ListMenuItemsUseCase uc, CancellationToken ct) =>
        {
            var items = await uc.ExecuteAsync(ct);
            return Results.Ok(items.Select(ToResponse));
        });

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, GetMenuItemUseCase uc, CancellationToken ct) =>
        {
            var item = await uc.ExecuteAsync(id, ct);
            return item is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found",
                    detail: $"Menu item {id} does not exist.")
                : Results.Ok(ToResponse(item));
        });

        group.MapPost("/", async Task<IResult> (CreateMenuItemBody body, CreateMenuItemUseCase uc, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["name"] = ["Name is required."] });
            if (body.Price <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["price"] = ["Price must be positive."] });
            if (string.IsNullOrWhiteSpace(body.Currency) || body.Currency.Length != 3)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["currency"] = ["Currency must be a 3-letter ISO code."] });

            var item = await uc.ExecuteAsync(
                new CreateMenuItemRequest(body.Name, body.SnackType, new Money(body.Price, body.Currency)), ct);
            return Results.Created($"/v1/menu/{item.Id}", ToResponse(item));
        }).RequireManager();

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateMenuItemBody body, GetMenuItemUseCase get,
            UpdateMenuItemUseCase update, CancellationToken ct) =>
        {
            if (await get.ExecuteAsync(id, ct) is null)
                return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found",
                    detail: $"Menu item {id} does not exist.");

            var item = await update.ExecuteAsync(
                new UpdateMenuItemRequest(
                    id,
                    body.NewName,
                    body.NewPrice.HasValue ? new Money(body.NewPrice.Value, body.NewCurrency ?? "USD") : null),
                ct);
            return Results.Ok(ToResponse(item));
        }).RequireManager();

        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, GetMenuItemUseCase get,
            RemoveMenuItemUseCase remove, CancellationToken ct) =>
        {
            if (await get.ExecuteAsync(id, ct) is null)
                return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found",
                    detail: $"Menu item {id} does not exist.");

            await remove.ExecuteAsync(id, ct);
            return Results.NoContent();
        }).RequireManager();

        return app;
    }

    private static MenuItemResponse ToResponse(MenuItem item) =>
        new(item.Id, item.Name, item.SnackType, item.Price.Amount, item.Price.Currency);
}
