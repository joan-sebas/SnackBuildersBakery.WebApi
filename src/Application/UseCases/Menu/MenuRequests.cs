using Domain;

namespace Application;

public sealed record CreateMenuItemRequest(string Name, SnackType SnackType, Money Price);

public sealed record UpdateMenuItemRequest(Guid Id, string? NewName, Money? NewPrice);
