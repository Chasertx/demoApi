namespace ModApi.Application.Entities;

/// <summary>
/// Describes an exposed entity so that a frontend can build lists and forms dynamically
/// (served at <c>GET api/metadata/entities</c>).
/// </summary>
/// <param name="Name">Entity name, e.g. <c>Product</c>.</param>
/// <param name="Route">Route segment, e.g. <c>products</c> (endpoints live under <c>api/{route}</c>).</param>
/// <param name="KeyType">Primary key type: <c>integer</c>, <c>uuid</c>, <c>string</c>...</param>
/// <param name="Properties">Column-like properties in display order.</param>
public sealed record EntityMetadata(string Name, string Route, string KeyType, IReadOnlyList<PropertyMetadata> Properties);

/// <summary>Describes a single entity property.</summary>
/// <param name="Name">JSON (camelCase) property name.</param>
/// <param name="Type">JSON-ish type: <c>string</c>, <c>integer</c>, <c>number</c>, <c>boolean</c>, <c>date-time</c>, <c>date</c>, <c>time</c>, <c>uuid</c>, <c>enum</c>, <c>binary</c>.</param>
/// <param name="IsNullable">Whether <c>null</c> is accepted.</param>
/// <param name="IsRequired">Whether a value must be supplied when creating/updating.</param>
/// <param name="IsReadOnly">Whether the server owns the value (key, audit fields).</param>
/// <param name="MaxLength">Maximum string length, if constrained.</param>
/// <param name="EnumValues">Allowed values for enum properties.</param>
public sealed record PropertyMetadata(
    string Name,
    string Type,
    bool IsNullable,
    bool IsRequired,
    bool IsReadOnly,
    int? MaxLength,
    IReadOnlyList<string>? EnumValues);
