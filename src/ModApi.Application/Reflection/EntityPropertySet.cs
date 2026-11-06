using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ModApi.Domain.Common;

namespace ModApi.Application.Reflection;

/// <summary>
/// Classifies the public properties of an entity:
/// <list type="bullet">
/// <item><b>Scalar</b> – column-like values (navigations and collections are excluded).</item>
/// <item><b>Queryable</b> – scalars that may be filtered and sorted.</item>
/// <item><b>Mutable</b> – scalars a client may write (excludes <c>Id</c>, audit fields and <c>[ReadOnly(true)]</c>).</item>
/// <item><b>Text</b> – string scalars used by free-text search.</item>
/// </list>
/// </summary>
public sealed class EntityPropertySet
{
    private static readonly HashSet<string> AuditPropertyNames =
        [nameof(IAuditable.CreatedAtUtc), nameof(IAuditable.UpdatedAtUtc)];

    private readonly Dictionary<string, PropertyInfo> _queryable;
    private readonly Dictionary<string, PropertyInfo> _mutable;
    private readonly HashSet<PropertyInfo> _nullable;
    private readonly HashSet<PropertyInfo> _system;

    internal EntityPropertySet(Type entityType)
    {
        EntityType = entityType;
        var nullability = new NullabilityInfoContext();
        var isAuditable = typeof(IAuditable).IsAssignableFrom(entityType);

        Scalar = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0
                        && p.GetMethod is { IsPublic: true }
                        && EntityProperties.IsScalarType(p.PropertyType)
                        && p.GetCustomAttribute<NotMappedAttribute>() is null)
            .DistinctBy(p => p.Name)
            .ToArray();

        _system = Scalar
            .Where(p => p.Name == "Id" || (isAuditable && AuditPropertyNames.Contains(p.Name)))
            .ToHashSet();

        _nullable = Scalar
            .Where(p => Nullable.GetUnderlyingType(p.PropertyType) is not null
                        || (!p.PropertyType.IsValueType && nullability.Create(p).WriteState != NullabilityState.NotNull))
            .ToHashSet();

        Mutable = Scalar
            .Where(p => p.SetMethod is { IsPublic: true }
                        && !_system.Contains(p)
                        && p.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly != true)
            .ToArray();

        Text = Scalar.Where(p => p.PropertyType == typeof(string)).ToArray();

        _queryable = Scalar
            .Where(p => p.PropertyType != typeof(byte[]))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _mutable = Mutable.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The entity type described.</summary>
    public Type EntityType { get; }

    /// <summary>Column-like properties.</summary>
    public IReadOnlyList<PropertyInfo> Scalar { get; }

    /// <summary>Properties a client may write.</summary>
    public IReadOnlyList<PropertyInfo> Mutable { get; }

    /// <summary>String properties used for free-text search.</summary>
    public IReadOnlyList<PropertyInfo> Text { get; }

    /// <summary>Finds a filterable/sortable property by name (case-insensitive).</summary>
    public bool TryGetQueryable(string name, [NotNullWhen(true)] out PropertyInfo? property) =>
        _queryable.TryGetValue(name, out property);

    /// <summary>Finds a client-writable property by name (case-insensitive).</summary>
    public bool TryGetMutable(string name, [NotNullWhen(true)] out PropertyInfo? property) =>
        _mutable.TryGetValue(name, out property);

    /// <summary>Whether the property accepts <c>null</c> (nullable value type or nullable reference type).</summary>
    public bool IsNullable(PropertyInfo property) => _nullable.Contains(property);

    /// <summary>Whether the property is managed by the server (<c>Id</c> or an audit field).</summary>
    public bool IsSystemProperty(PropertyInfo property) => _system.Contains(property);
}
