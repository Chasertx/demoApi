using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using ModApi.Application.Reflection;

namespace ModApi.Application.Entities;

public sealed class EntityMetadataProvider(IEntityRegistry registry) : IEntityMetadataProvider
{
    private readonly Lazy<List<EntityMetadata>> _metadata = new(() => Build(registry));

    public IReadOnlyList<EntityMetadata> GetAll() => _metadata.Value;

    public EntityMetadata? Find(string route) =>
        _metadata.Value.FirstOrDefault(m => string.Equals(m.Route, route.Trim('/'), StringComparison.OrdinalIgnoreCase));

    public static string DescribeType(Type type)
    {
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

    private static List<EntityMetadata> Build(IEntityRegistry registry) =>
        registry.Entities
            .Where(d => d.IsExposed)
            .Select(d => new EntityMetadata(d.Name, d.Route, DescribeType(d.KeyType), DescribeProperties(d.EntityType)))
            .ToList();

    private static List<PropertyMetadata> DescribeProperties(Type entityType)
    {
        var properties = EntityProperties.For(entityType);
        var mutable = properties.Mutable.ToHashSet();

        // id first, audit columns last, everything else in declaration order
        return properties.Scalar
            .OrderBy(p => p.Name == "Id" ? 0 : properties.IsSystemProperty(p) ? 2 : 1)
            .Select(p =>
            {
                var isNullable = properties.IsNullable(p);
                var isReadOnly = !mutable.Contains(p);
                var isRequired = p.GetCustomAttribute<RequiredAttribute>() is not null
                    || (!isReadOnly && !isNullable && !p.PropertyType.IsValueType);
                var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var maxLength = p.GetCustomAttribute<MaxLengthAttribute>()?.Length
                    ?? p.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;

                return new PropertyMetadata(
                    JsonNamingPolicy.CamelCase.ConvertName(p.Name),
                    DescribeType(p.PropertyType),
                    isNullable,
                    isRequired,
                    isReadOnly,
                    maxLength,
                    underlying.IsEnum ? Enum.GetNames(underlying) : null);
            })
            .ToList();
    }
}
