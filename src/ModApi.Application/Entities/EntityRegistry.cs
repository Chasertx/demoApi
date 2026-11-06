using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ModApi.Domain.Common;

namespace ModApi.Application.Entities;

public sealed class EntityRegistry : IEntityRegistry
{
    private readonly Dictionary<Type, EntityDescriptor> _byType;
    private readonly Dictionary<string, EntityDescriptor> _byRoute;

    public EntityRegistry(IEnumerable<Assembly> assemblies)
        : this(RequireAny(assemblies))
    {
    }

    public EntityRegistry(IEnumerable<Type> entityTypes)
        : this(entityTypes, null)
    {
    }

    private EntityRegistry(Assembly[] assemblies)
        : this(assemblies.SelectMany(GetLoadableTypes).Where(IsEntityType), assemblies)
    {
    }

    private EntityRegistry(IEnumerable<Type> entityTypes, IReadOnlyList<Assembly>? assemblies)
    {
        var descriptors = entityTypes
            .Distinct()
            .Select(CreateDescriptor)
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .ToList();

        var duplicate = descriptors
            .GroupBy(d => d.Route, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            var types = string.Join(", ", duplicate.Select(d => d.EntityType.FullName));
            throw new InvalidOperationException(
                $"Route '{duplicate.Key}' is used by more than one entity ({types}). Give one of them a [CrudResource(\"...\")] route.");
        }

        Entities = descriptors;
        Assemblies = assemblies ?? descriptors.Select(d => d.EntityType.Assembly).Distinct().ToArray();
        Signature = string.Join(';', descriptors.Select(d => d.EntityType.AssemblyQualifiedName));
        _byType = descriptors.ToDictionary(d => d.EntityType);
        _byRoute = descriptors.ToDictionary(d => d.Route, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<EntityDescriptor> Entities { get; }

    public IReadOnlyList<Assembly> Assemblies { get; }

    public string Signature { get; }

    public bool TryGetByType(Type entityType, [NotNullWhen(true)] out EntityDescriptor? descriptor) =>
        _byType.TryGetValue(entityType, out descriptor);

    public bool TryGetByRoute(string route, [NotNullWhen(true)] out EntityDescriptor? descriptor) =>
        _byRoute.TryGetValue(route.Trim('/'), out descriptor);

    public static bool TryGetKeyType(Type type, [NotNullWhen(true)] out Type? keyType)
    {
        var keyTypes = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToArray();

        keyType = keyTypes.Length == 1 ? keyTypes[0] : null;
        return keyType is not null;
    }

    public static bool IsEntityType(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false } &&
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

    private static EntityDescriptor CreateDescriptor(Type type)
    {
        if (!IsEntityType(type))
        {
            throw new ArgumentException($"'{type.FullName}' is not a concrete class implementing IEntity<TKey>.", nameof(type));
        }

        if (!TryGetKeyType(type, out var keyType))
        {
            throw new InvalidOperationException($"'{type.FullName}' implements IEntity<TKey> more than once.");
        }

        var attribute = type.GetCustomAttribute<CrudResourceAttribute>(inherit: false);
        var route = string.IsNullOrWhiteSpace(attribute?.Route)
            ? RouteNaming.ToRoute(type.Name)
            : attribute.Route.Trim().Trim('/').ToLowerInvariant();

        return new EntityDescriptor(type, keyType, type.Name, route, attribute?.Expose ?? true);
    }

    private static Assembly[] RequireAny(IEnumerable<Assembly> assemblies)
    {
        var list = assemblies.Distinct().ToArray();
        if (list.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
        }

        return list;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
