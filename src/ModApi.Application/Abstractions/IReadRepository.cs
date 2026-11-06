using ModApi.Domain.Common;

namespace ModApi.Application.Abstractions;

/// <summary>
/// Read-side persistence port. The application layer composes LINQ queries with
/// <c>query</c> callbacks; the infrastructure layer executes them against the database.
/// </summary>
public interface IReadRepository<TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    /// <summary>Executes the (optionally shaped) query and returns the materialized, untracked results.</summary>
    Task<IReadOnlyList<TEntity>> ListAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Counts the rows matched by the (optionally shaped) query.</summary>
    Task<int> CountAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Finds an entity by primary key. The returned instance is tracked for updates.</summary>
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);
}
