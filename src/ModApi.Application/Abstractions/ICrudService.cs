using System.Text.Json;
using ModApi.Application.Querying;
using ModApi.Domain.Common;

namespace ModApi.Application.Abstractions;

public interface ICrudService<TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    Task<PagedResult<TEntity>> GetPagedAsync(QueryParameters parameters, CancellationToken cancellationToken = default);

    Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity> PatchAsync(TKey id, IReadOnlyDictionary<string, JsonElement> patch, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
