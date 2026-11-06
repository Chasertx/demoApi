using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ModApi.Application.Entities;

namespace ModApi.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context. It has no hard-coded <c>DbSet</c> properties: the model is built from the
/// <see cref="IEntityRegistry"/>, so adding an entity class is enough to persist it.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IEntityRegistry entityRegistry)
        : base(options)
    {
        EntityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
    }

    /// <summary>The registry describing which entities are part of the model.</summary>
    public IEntityRegistry EntityRegistry { get; }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // The model depends on the registry, so the cache key must too.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, EntityRegistryModelCacheKeyFactory>();
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // A sensible default for money-like values on every provider; override per property with HasPrecision.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var registered = EntityRegistry.Entities.Select(e => e.EntityType).ToHashSet();
        foreach (var entityType in registered)
        {
            modelBuilder.Entity(entityType);
        }

        // Apply IEntityTypeConfiguration<T> classes from entity assemblies and this assembly – but only for registered entities.
        foreach (var assembly in EntityRegistry.Assemblies.Append(typeof(AppDbContext).Assembly).Distinct())
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly, type => ConfiguresRegisteredEntity(type, registered));
        }
    }

    private static bool ConfiguresRegisteredEntity(Type configurationType, HashSet<Type> registered) =>
        configurationType.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>) &&
            registered.Contains(i.GetGenericArguments()[0]));
}
