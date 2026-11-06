using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ModApi.Domain.Common;

namespace ModApi.Application.Entities;

/// <summary>
/// Discovers entities by scanning assemblies for non-abstract classes implementing <see cref="IEntity{TKey}"/>.
/// The registry is immutable once built, so it is safe to share as a singleton.
/// </summary>
public sealed class EntityRegistry : IEntityRegistry
{
    private readonly Dictionary<Type, EntityDescriptor> _byType;
    private readonly Dictionary<string, EntityDescriptor> _byRoute;

    /// <summary>Scans the given assemblies for entity types.</summary>
    public EntityRegistry(IEnumerable<Assembly> assemblies)
        : this(ScanAssemblies(assemblies), RequireAssemblies(assemblies))
    {
    }

    /// <summary>Registers an explicit set of entity types (useful for tests and modular hosts).</summary>
    public EntityRegistry(IEnumerable<Type> entityTypes)
        : this(RequireEntityTypes(entityTypes), null)
    {
    }

    private EntityRegistry(IReadOnlyCollection<Type> entityTypes, IReadOnlyList<Assembly>? assemblies)
    {
        var descriptors = entityTypes
            .Distinct()
            .Select(CreateDescriptor)
            .OrderBy(descriptor => descriptor.Name, StringComparer.Ordinal)
            .ToList();

        var duplicateRoute = descriptors
            .GroupBy(descriptor => descriptor.Route, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRoute is not null)
        {
            throw new InvalidOperationException(
                $"Route '{duplicateRoute.Key}' is used by more than one entity: " +
                $"{string.Join(", ", duplicateRoute.Select(d => d.EntityType.FullName))}. " +
                $"Use [{nameof(CrudResourceAttribute)}(\"...\")] to give one of them a unique route.");
        }

        Entities = descriptors;
        Assemblies = assemblies ?? descriptors.Select(d => d.EntityType.Assembly).Distinct().ToArray();
        Signature = string.Join(';', descriptors.Select(d => d.EntityType.AssemblyQualifiedName));
        _byType = descriptors.ToDictionary(d => d.EntityType);
        _byRoute = descriptors.ToDictionary(d => d.Route, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<EntityDescriptor> Entities { get; }

    /// <inheritdoc />
    public IReadOnlyList<Assembly> Assemblies { get; }

    /// <inheritdoc />
    public string Signature { get; }

    /// <inheritdoc />
    public bool TryGetByType(Type entityType, [NotNullWhen(true)] out EntityDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return _byType.TryGetValue(entityType, out descriptor);
    }

    /// <inheritdoc />
    public bool TryGetByRoute(string route, [NotNullWhen(true)] out EntityDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(route);
        return _byRoute.TryGetValue(route.Trim('/'), out descriptor);
    }

    /// <summary>
    /// Returns the <c>TKey</c> of the single <see cref="IEntity{TKey}"/> implemented by <paramref name="type"/>.
    /// </summary>
    public static bool TryGetKeyType(Type type, [NotNullWhen(true)] out Type? keyType)
    {
        ArgumentNullException.ThrowIfNull(type);

        var keyTypes = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToArray();

        keyType = keyTypes.Length == 1 ? keyTypes[0] : null;
        return keyType is not null;
    }

    /// <summary>Whether <paramref name="type"/> qualifies as an entity (concrete, non-generic class implementing <see cref="IEntity{TKey}"/>).</summary>
    public static bool IsEntityType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false } &&
               type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
    }

    private static EntityDescriptor CreateDescriptor(Type type)
    {
        if (!IsEntityType(type))
        {
            throw new ArgumentException(
                $"'{type.FullName}' is not a concrete, non-generic class implementing IEntity<TKey>.", nameof(type));
        }

        if (!TryGetKeyType(type, out var keyType))
        {
            throw new InvalidOperationException(
                $"'{type.FullName}' implements IEntity<TKey> more than once; an entity must have exactly one key type.");
        }

        var attribute = type.GetCustomAttribute<CrudResourceAttribute>(inherit: false);
        var route = string.IsNullOrWhiteSpace(attribute?.Route)
            ? RouteNaming.ToRoute(type.Name)
            : attribute.Route.Trim().Trim('/').ToLowerInvariant();

        return new EntityDescriptor(type, keyType, type.Name, route, attribute?.Expose ?? true);
    }

    private static IReadOnlyList<Assembly> RequireAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        var list = assemblies.Distinct().ToArray();
        if (list.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
        }

        return list;
    }

    private static IReadOnlyCollection<Type> ScanAssemblies(IEnumerable<Assembly> assemblies) =>
        RequireAssemblies(assemblies)
            .SelectMany(GetLoadableTypes)
            .Where(IsEntityType)
            .ToArray();

    private static IReadOnlyCollection<Type> RequireEntityTypes(IEnumerable<Type> entityTypes)
    {
        ArgumentNullException.ThrowIfNull(entityTypes);
        return entityTypes.ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
