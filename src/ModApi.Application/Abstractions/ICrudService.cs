using System.Text.Json;
using ModApi.Application.Querying;
using ModApi.Domain.Common;

namespace ModApi.Application.Abstractions;

/// <summary>Generic create/read/update/delete use cases for any registered entity.</summary>
public interface ICrudService<TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    /// <summary>Returns a filtered, searched, sorted page of entities.</summary>
    /// <exception cref="Exceptions.RequestValidationException">The query parameters are invalid.</exception>
    Task<PagedResult<TEntity>> GetPagedAsync(QueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>Returns a single entity.</summary>
    /// <exception cref="Exceptions.NotFoundException">No entity has the given key.</exception>
    Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Creates an entity. Store-generated keys and audit fields are assigned by the server.</summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Replaces every writable property of an existing entity (HTTP PUT semantics).</summary>
    Task<TEntity> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Changes only the supplied properties of an existing entity (JSON merge-patch semantics).</summary>
    Task<TEntity> PatchAsync(TKey id, IReadOnlyDictionary<string, JsonElement> patch, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entity.</summary>
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
