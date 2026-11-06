using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using ModApi.Application.Reflection;

namespace ModApi.Application.Entities;

/// <summary>Builds <see cref="EntityMetadata"/> from the registry once and caches it.</summary>
public sealed class EntityMetadataProvider : IEntityMetadataProvider
{
    private readonly Lazy<IReadOnlyList<EntityMetadata>> _metadata;

    public EntityMetadataProvider(IEntityRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _metadata = new Lazy<IReadOnlyList<EntityMetadata>>(() => Build(registry));
    }

    /// <inheritdoc />
    public IReadOnlyList<EntityMetadata> GetAll() => _metadata.Value;

    /// <inheritdoc />
    public EntityMetadata? Find(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return _metadata.Value.FirstOrDefault(m => string.Equals(m.Route, route.Trim('/'), StringComparison.OrdinalIgnoreCase));
    }

    private static List<EntityMetadata> Build(IEntityRegistry registry) =>
        registry.Entities
            .Where(d => d.IsExposed)
            .Select(d => new EntityMetadata(d.Name, d.Route, DescribeType(d.KeyType), DescribeProperties(d.EntityType)))
            .ToList();

    private static List<PropertyMetadata> DescribeProperties(Type entityType)
    {
        var properties = EntityProperties.For(entityType);
        var mutable = properties.Mutable.ToHashSet();

        return properties.Scalar
            .OrderBy(p => p.Name == "Id" ? 0 : properties.IsSystemProperty(p) ? 2 : 1)
            .Select(p =>
            {
                var isNullable = properties.IsNullable(p);
                var isReadOnly = !mutable.Contains(p);
                var isRequired = p.GetCustomAttribute<RequiredAttribute>() is not null
                                 || (!isReadOnly && !isNullable && !p.PropertyType.IsValueType);
                var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                return new PropertyMetadata(
                    JsonNamingPolicy.CamelCase.ConvertName(p.Name),
                    DescribeType(p.PropertyType),
                    isNullable,
                    isRequired,
                    isReadOnly,
                    p.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? p.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength,
                    underlying.IsEnum ? Enum.GetNames(underlying) : null);
            })
            .ToList();
    }

    /// <summary>Maps a CLR type to a JSON-schema-like type name.</summary>
    public static string DescribeType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t.IsEnum)
        {
            return "enum";
        }

        return Type.GetTypeCode(t) switch
        {
            TypeCode.Boolean => "boolean",
            TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
                TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => "integer",
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
            TypeCode.DateTime => "date-time",
            _ when t == typeof(DateTimeOffset) => "date-time",
            _ when t == typeof(DateOnly) => "date",
            _ when t == typeof(TimeOnly) || t == typeof(TimeSpan) => "time",
            _ when t == typeof(Guid) => "uuid",
            _ when t == typeof(byte[]) => "binary",
            _ => "string",
        };
    }
}
