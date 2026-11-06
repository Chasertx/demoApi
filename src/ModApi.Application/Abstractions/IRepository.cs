using ModApi.Domain.Common;

namespace ModApi.Application.Abstractions;

/// <summary>
/// Read/write persistence port. Writes are staged and committed through <see cref="IUnitOfWork"/>.
/// </summary>
public interface IRepository<TEntity, in TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    /// <summary>Stages a new entity for insertion.</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Stages an entity for deletion.</summary>
    void Remove(TEntity entity);
}
