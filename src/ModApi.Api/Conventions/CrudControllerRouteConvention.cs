using Microsoft.AspNetCore.Mvc.ApplicationModels;
using ModApi.Application.Entities;

namespace ModApi.Api.Conventions;

/// <summary>
/// Names each CRUD controller after its entity route so <c>[Route("api/[controller]")]</c> resolves to
/// <c>api/products</c>, and Swagger groups operations under a readable tag.
/// </summary>
public sealed class CrudControllerRouteConvention : IControllerModelConvention
{
    private readonly IEntityRegistry _registry;

    public CrudControllerRouteConvention(IEntityRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public void Apply(ControllerModel controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (CrudControllerTypes.TryGetEntityType(controller.ControllerType, out var entityType) &&
            _registry.TryGetByType(entityType, out var descriptor))
        {
            controller.ControllerName = descriptor.Route;
        }
    }
}
