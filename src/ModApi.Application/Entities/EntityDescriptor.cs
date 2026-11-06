namespace ModApi.Application.Entities;

/// <summary>Describes a discovered entity and how it is exposed.</summary>
/// <param name="EntityType">The CLR entity type.</param>
/// <param name="KeyType">The primary key type (the <c>TKey</c> of <c>IEntity&lt;TKey&gt;</c>).</param>
/// <param name="Name">The entity name (CLR type name).</param>
/// <param name="Route">The URL segment under <c>api/</c>.</param>
/// <param name="IsExposed">Whether CRUD endpoints are generated for the entity.</param>
public sealed record EntityDescriptor(Type EntityType, Type KeyType, string Name, string Route, bool IsExposed);
