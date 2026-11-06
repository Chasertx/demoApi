using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModApi.Application.Abstractions;
using ModApi.Application.Entities;
using ModApi.Application.Mapping;
using ModApi.Application.Services;

namespace ModApi.Application;

public static class DependencyInjection
{
    // TryAdd so an app can register its own ICrudService<Order, int> (or mapper) before calling this.
    public static IServiceCollection AddApplication(this IServiceCollection services, IEntityRegistry entityRegistry)
    {
        services.TryAddSingleton(entityRegistry);
        services.TryAddSingleton<IEntityMapper>(_ => new ReflectionEntityMapper());
        services.TryAddSingleton<IEntityMetadataProvider, EntityMetadataProvider>();
        services.TryAddScoped(typeof(ICrudService<,>), typeof(CrudService<,>));

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] entityAssemblies) =>
        services.AddApplication(new EntityRegistry(entityAssemblies));
}
