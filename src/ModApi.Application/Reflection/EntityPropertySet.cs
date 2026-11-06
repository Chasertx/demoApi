using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ModApi.Domain.Common;

namespace ModApi.Application.Reflection;

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

        // Only column-like properties; navigations, indexers and [NotMapped] are left out.
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

        // Clients may never write keys, audit columns or anything marked [ReadOnly(true)].
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

    public Type EntityType { get; }

    public IReadOnlyList<PropertyInfo> Scalar { get; }

    public IReadOnlyList<PropertyInfo> Mutable { get; }

    public IReadOnlyList<PropertyInfo> Text { get; }

    public bool TryGetQueryable(string name, [NotNullWhen(true)] out PropertyInfo? property) =>
        _queryable.TryGetValue(name, out property);

    public bool TryGetMutable(string name, [NotNullWhen(true)] out PropertyInfo? property) =>
        _mutable.TryGetValue(name, out property);

    public bool IsNullable(PropertyInfo property) => _nullable.Contains(property);

    public bool IsSystemProperty(PropertyInfo property) => _system.Contains(property);
}
