using System.ComponentModel.DataAnnotations;
using Domain;

namespace Api.Contracts;

public sealed record CreateMenuItemBody(
    [Required(AllowEmptyStrings = false)] string Name,
    SnackType SnackType,
    [Range(0.01, double.MaxValue)] decimal Price,
    [Required][StringLength(3, MinimumLength = 3)] string Currency);

public sealed record UpdateMenuItemBody(
    string? NewName,
    decimal? NewPrice,
    string? NewCurrency);

public sealed record MenuItemResponse(
    Guid Id,
    string Name,
    SnackType SnackType,
    decimal Price,
    string Currency);
