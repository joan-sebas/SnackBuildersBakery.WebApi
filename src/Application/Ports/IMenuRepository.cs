using Domain;

namespace Application;

public interface IMenuRepository
{
    Task<IReadOnlyList<MenuItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MenuItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(MenuItem item, CancellationToken cancellationToken = default);
}
