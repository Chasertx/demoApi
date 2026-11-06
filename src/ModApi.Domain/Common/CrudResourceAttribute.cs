namespace ModApi.Domain.Common;

/// <summary>
/// Optional metadata controlling how an entity is exposed through the generic CRUD API.
/// Without this attribute an entity is exposed at <c>api/{kebab-case-plural-name}</c>,
/// e.g. <c>OrderLine</c> becomes <c>api/order-lines</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CrudResourceAttribute : Attribute
{
    /// <summary>Uses the conventional route.</summary>
    public CrudResourceAttribute()
    {
    }

    /// <summary>Uses a custom route segment, e.g. <c>"catalog-items"</c> for <c>api/catalog-items</c>.</summary>
    public CrudResourceAttribute(string route)
    {
        Route = route;
    }

    /// <summary>The custom route segment, or <c>null</c> for the conventional one.</summary>
    public string? Route { get; }

    /// <summary>
    /// When <c>false</c> the entity is still part of the database model but no endpoints are generated.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool Expose { get; set; } = true;
}
