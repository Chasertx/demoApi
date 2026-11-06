using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;
using ModApi.Application.Reflection;

namespace ModApi.Application.Mapping;

/// <summary>
/// Reflection-based <see cref="IEntityMapper"/>. Only <see cref="EntityPropertySet.Mutable"/> properties are
/// ever written, so keys and audit fields cannot be overwritten by clients (mass-assignment protection).
/// </summary>
public sealed class ReflectionEntityMapper : IEntityMapper
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>Uses web JSON defaults with string enums, matching the API serializer.</summary>
    public ReflectionEntityMapper()
        : this(CreateDefaultJsonOptions())
    {
    }

    /// <summary>Uses custom JSON options for deserializing patch values.</summary>
    public ReflectionEntityMapper(JsonSerializerOptions jsonOptions)
    {
        _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
    }

    /// <inheritdoc />
    public void CopyValues<TEntity>(TEntity source, TEntity target)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        foreach (var property in EntityProperties.For(typeof(TEntity)).Mutable)
        {
            property.SetValue(target, property.GetValue(source));
        }
    }

    /// <inheritdoc />
    public void ApplyPatch<TEntity>(TEntity target, IReadOnlyDictionary<string, JsonElement> patch)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(patch);

        var properties = EntityProperties.For(typeof(TEntity));
        var errors = new ValidationErrorCollector();
        var pending = new List<(System.Reflection.PropertyInfo Property, object? Value)>();

        foreach (var (name, element) in patch)
        {
            if (!properties.TryGetMutable(name, out var property))
            {
                errors.Add(name, properties.TryGetQueryable(name, out _)
                    ? $"'{name}' is read-only."
                    : $"'{name}' is not a known property of {typeof(TEntity).Name}.");
                continue;
            }

            object? value;
            try
            {
                value = element.Deserialize(property.PropertyType, _jsonOptions);
            }
            catch (JsonException)
            {
                errors.Add(ToJsonName(property.Name), $"The value is not valid for '{property.Name}'.");
                continue;
            }

            if (value is null && !properties.IsNullable(property))
            {
                errors.Add(ToJsonName(property.Name), $"'{property.Name}' cannot be null.");
                continue;
            }

            pending.Add((property, value));
        }

        errors.ThrowIfAny();

        foreach (var (property, value) in pending)
        {
            property.SetValue(target, value);
        }

        Validate(target);
    }

    private static void Validate(object entity)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(entity, new ValidationContext(entity), results, validateAllProperties: true))
        {
            return;
        }

        var errors = new ValidationErrorCollector();
        foreach (var result in results)
        {
            var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
            foreach (var member in members)
            {
                errors.Add(ToJsonName(member), result.ErrorMessage ?? "The value is invalid.");
            }
        }

        errors.ThrowIfAny();
    }

    private static string ToJsonName(string propertyName) => JsonNamingPolicy.CamelCase.ConvertName(propertyName);

    private static JsonSerializerOptions CreateDefaultJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
