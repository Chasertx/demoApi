using ModApi.Domain.Common;

namespace ModApi.Application.Abstractions;

public interface IReadRepository<TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    Task<IReadOnlyList<TEntity>> ListAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default);

    // Returns a tracked entity so callers can modify it and save through IUnitOfWork.
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);
}
