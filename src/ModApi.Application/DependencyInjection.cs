using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModApi.Application.Abstractions;
using ModApi.Application.Entities;
using ModApi.Application.Mapping;
using ModApi.Application.Services;

namespace ModApi.Application;

/// <summary>Registers application-layer services.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the entity registry, mapper, metadata provider and the open-generic CRUD service.
    /// <c>TryAdd*</c> is used throughout so hosts can register their own implementations first.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IEntityRegistry entityRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(entityRegistry);

        services.TryAddSingleton(entityRegistry);
        services.TryAddSingleton<IEntityMapper>(_ => new ReflectionEntityMapper());
        services.TryAddSingleton<IEntityMetadataProvider, EntityMetadataProvider>();
        services.TryAddScoped(typeof(ICrudService<,>), typeof(CrudService<,>));

        return services;
    }

    /// <summary>Scans <paramref name="entityAssemblies"/> for entities and registers application services.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] entityAssemblies) =>
        services.AddApplication(new EntityRegistry(entityAssemblies));
}
