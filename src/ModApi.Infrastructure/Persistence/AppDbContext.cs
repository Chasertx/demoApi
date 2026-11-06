using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ModApi.Application.Entities;

namespace ModApi.Infrastructure.Persistence;

// No DbSet properties on purpose: the model is built from whatever is in the entity registry.
public class AppDbContext(DbContextOptions<AppDbContext> options, IEntityRegistry entityRegistry) : DbContext(options)
{
    public IEntityRegistry EntityRegistry { get; } = entityRegistry;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, EntityRegistryModelCacheKeyFactory>();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var registered = EntityRegistry.Entities.Select(e => e.EntityType).ToHashSet();
        foreach (var entityType in registered)
        {
            modelBuilder.Entity(entityType);
        }

        var assemblies = EntityRegistry.Assemblies.Append(typeof(AppDbContext).Assembly).Distinct();
        foreach (var assembly in assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly, type => ConfiguresAnyOf(type, registered));
        }
    }

    private static bool ConfiguresAnyOf(Type configurationType, HashSet<Type> entityTypes) =>
        configurationType.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>) &&
            entityTypes.Contains(i.GetGenericArguments()[0]));
}
