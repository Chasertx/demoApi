using System.Collections.Concurrent;

namespace ModApi.Application.Reflection;

public static class EntityProperties
{
    private static readonly ConcurrentDictionary<Type, EntityPropertySet> Cache = new();

    public static EntityPropertySet For(Type entityType) =>
        Cache.GetOrAdd(entityType, static type => new EntityPropertySet(type));

    public static bool IsScalarType(Type type)
    {
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
