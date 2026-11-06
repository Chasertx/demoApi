namespace ModApi.Application.Entities;

public sealed record EntityDescriptor(Type EntityType, Type KeyType, string Name, string Route, bool IsExposed);
