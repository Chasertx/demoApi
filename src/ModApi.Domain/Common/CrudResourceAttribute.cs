namespace ModApi.Domain.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CrudResourceAttribute : Attribute
{
    public CrudResourceAttribute()
    {
    }

    public CrudResourceAttribute(string route)
    {
        Route = route;
    }

    public string? Route { get; }

    // false keeps the entity in the database model but skips generating endpoints for it
    public bool Expose { get; set; } = true;
}
