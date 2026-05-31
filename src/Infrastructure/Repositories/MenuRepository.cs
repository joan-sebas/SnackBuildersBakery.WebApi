using Application;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

internal sealed class MenuRepository(AppDbContext db) : IMenuRepository
{
    public async Task<IReadOnlyList<MenuItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.MenuItems
            .Where(m => !m.IsRemoved)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

    public async Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.MenuItems.FindAsync([id], cancellationToken);

    public async Task AddAsync(MenuItem item, CancellationToken cancellationToken = default)
    {
        db.MenuItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MenuItem item, CancellationToken cancellationToken = default) =>
        await db.SaveChangesAsync(cancellationToken);
}
