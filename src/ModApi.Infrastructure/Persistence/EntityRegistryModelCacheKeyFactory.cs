using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ModApi.Infrastructure.Persistence;

/// <summary>
/// Includes the entity registry signature in EF Core's model cache key, so contexts built with different
/// entity sets (e.g. modular hosts or tests) never share a cached model.
/// </summary>
public sealed class EntityRegistryModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc />
    public object Create(DbContext context, bool designTime) =>
        context is AppDbContext appContext
            ? (context.GetType(), appContext.EntityRegistry.Signature, designTime)
            : (context.GetType(), designTime);
}
