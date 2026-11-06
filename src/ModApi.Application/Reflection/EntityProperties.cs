using System.Collections.Concurrent;

namespace ModApi.Application.Reflection;

/// <summary>Thread-safe cache of per-entity property metadata.</summary>
public static class EntityProperties
{
    private static readonly ConcurrentDictionary<Type, EntityPropertySet> Cache = new();

    /// <summary>Returns (and caches) the property metadata for <paramref name="entityType"/>.</summary>
    public static EntityPropertySet For(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return Cache.GetOrAdd(entityType, static type => new EntityPropertySet(type));
    }

    /// <summary>
    /// Whether a property of this type is a column-like scalar value (as opposed to a navigation or collection).
    /// </summary>
    public static bool IsScalarType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive
            || t.IsEnum
            || t == typeof(string)
            || t == typeof(decimal)
            || t == typeof(DateTime)
            || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly)
            || t == typeof(TimeOnly)
            || t == typeof(TimeSpan)
            || t == typeof(Guid)
            || t == typeof(byte[]);
    }
}
