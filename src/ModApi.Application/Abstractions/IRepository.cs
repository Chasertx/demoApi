using ModApi.Domain.Common;

namespace ModApi.Application.Abstractions;

public interface IRepository<TEntity, in TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);
}
