using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Inserts a baseline menu when the database has no menu items.
/// Idempotent: skips if any rows already exist in the menu_items table.
/// </summary>
public static class MenuItemSeed
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.MenuItems.AnyAsync(cancellationToken))
            return;

        db.MenuItems.AddRange(
            new MenuItem(new Guid("a1b2c3d4-0001-0000-0000-000000000000"), "Chocolate Chip Cookie", SnackType.Cookie, new Money(3.50m, "USD")),
            new MenuItem(new Guid("a1b2c3d4-0002-0000-0000-000000000000"), "Oatmeal Cookie", SnackType.Cookie, new Money(3.00m, "USD")),
            new MenuItem(new Guid("a1b2c3d4-0003-0000-0000-000000000000"), "Butter Croissant", SnackType.Pastry, new Money(4.50m, "USD")),
            new MenuItem(new Guid("a1b2c3d4-0004-0000-0000-000000000000"), "Cinnamon Roll", SnackType.Pastry, new Money(4.00m, "USD")),
            new MenuItem(new Guid("a1b2c3d4-0005-0000-0000-000000000000"), "Sourdough Loaf", SnackType.Bread, new Money(6.00m, "USD")));

        await db.SaveChangesAsync(cancellationToken);
    }
}
