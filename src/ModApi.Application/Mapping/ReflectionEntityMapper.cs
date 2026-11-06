using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;
using ModApi.Application.Reflection;

namespace ModApi.Application.Mapping;

public sealed class ReflectionEntityMapper(JsonSerializerOptions jsonOptions) : IEntityMapper
{
    public ReflectionEntityMapper()
        : this(DefaultJsonOptions())
    {
    }

    public void CopyValues<TEntity>(TEntity source, TEntity target)
        where TEntity : class
    {
        foreach (var property in EntityProperties.For(typeof(TEntity)).Mutable)
        {
            property.SetValue(target, property.GetValue(source));
        }
    }

    public void ApplyPatch<TEntity>(TEntity target, IReadOnlyDictionary<string, JsonElement> patch)
        where TEntity : class
    {
        var properties = EntityProperties.For(typeof(TEntity));
        var errors = new ValidationErrorCollector();
        var changes = new List<(PropertyInfo Property, object? Value)>();

        foreach (var (name, element) in patch)
        {
            if (!properties.TryGetMutable(name, out var property))
            {
                var reason = properties.TryGetQueryable(name, out _)
                    ? $"'{name}' is read-only."
                    : $"'{name}' is not a known property of {typeof(TEntity).Name}.";
                errors.Add(name, reason);
                continue;
            }

            var key = JsonName(property.Name);
            object? value;
            try
            {
                value = element.Deserialize(property.PropertyType, jsonOptions);
            }
            catch (JsonException)
            {
                errors.Add(key, $"The value is not valid for '{property.Name}'.");
                continue;
            }

            if (value is null && !properties.IsNullable(property))
            {
                errors.Add(key, $"'{property.Name}' cannot be null.");
                continue;
            }

            changes.Add((property, value));
        }

        // Validate the whole patch before touching the entity.
        errors.ThrowIfAny();

        foreach (var (property, value) in changes)
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
                errors.Add(JsonName(member), result.ErrorMessage ?? "The value is invalid.");
            }
        }

        errors.ThrowIfAny();
    }

    private static string JsonName(string propertyName) => JsonNamingPolicy.CamelCase.ConvertName(propertyName);

    private static JsonSerializerOptions DefaultJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
