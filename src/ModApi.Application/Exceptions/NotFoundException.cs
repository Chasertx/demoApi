namespace ModApi.Application.Exceptions;

/// <summary>Thrown when a requested entity does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    /// <summary>The entity type name.</summary>
    public string EntityName { get; }

    /// <summary>The key that was looked up.</summary>
    public object Key { get; }
}
