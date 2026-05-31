using Domain;

namespace Application;

public sealed class CreateMenuItemUseCase(IMenuRepository menu)
{
    public async Task<MenuItem> ExecuteAsync(
        CreateMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = new MenuItem(Guid.NewGuid(), request.Name, request.SnackType, request.Price);
        await menu.AddAsync(item, cancellationToken);
        return item;
    }
}

public sealed class GetMenuItemUseCase(IMenuRepository menu)
{
    public Task<MenuItem?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
        menu.GetByIdAsync(id, cancellationToken);
}

public sealed class UpdateMenuItemUseCase(IMenuRepository menu)
{
    public async Task<MenuItem> ExecuteAsync(
        UpdateMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await menu.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Menu item not found. Id: {request.Id}");

        if (request.NewName is not null)
        {
            item.Rename(request.NewName);
        }

        if (request.NewPrice is { } price)
        {
            item.Reprice(price);
        }

        await menu.UpdateAsync(item, cancellationToken);
        return item;
    }
}

public sealed class RemoveMenuItemUseCase(IMenuRepository menu)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await menu.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Menu item not found. Id: {id}");

        item.Remove();
        await menu.UpdateAsync(item, cancellationToken);
    }
}

public sealed class ListMenuItemsUseCase(IMenuRepository menu)
{
    public async Task<IReadOnlyList<MenuItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var all = await menu.ListAsync(cancellationToken);
        return all.Where(i => !i.IsRemoved).ToList();
    }
}
