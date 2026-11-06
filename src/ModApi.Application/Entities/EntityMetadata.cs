namespace ModApi.Application.Entities;

public sealed record EntityMetadata(string Name, string Route, string KeyType, IReadOnlyList<PropertyMetadata> Properties);

public sealed record PropertyMetadata(
    string Name,
    string Type,
    bool IsNullable,
    bool IsRequired,
    bool IsReadOnly,
    int? MaxLength,
    IReadOnlyList<string>? EnumValues);
