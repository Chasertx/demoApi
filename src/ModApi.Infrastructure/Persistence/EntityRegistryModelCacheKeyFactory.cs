using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ModApi.Infrastructure.Persistence;

public sealed class EntityRegistryModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is AppDbContext appContext
            ? (context.GetType(), appContext.EntityRegistry.Signature, designTime)
            : (context.GetType(), designTime);
}
