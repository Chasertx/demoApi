using Microsoft.AspNetCore.Mvc.ApplicationModels;
using ModApi.Application.Entities;

namespace ModApi.Api.Conventions;

// Renames each CRUD controller to its entity route so "api/[controller]" becomes "api/products".
public sealed class CrudControllerRouteConvention(IEntityRegistry registry) : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (CrudControllerTypes.TryGetEntityType(controller.ControllerType, out var entityType) &&
            registry.TryGetByType(entityType, out var entity))
        {
            controller.ControllerName = entity.Route;
        }
    }
}
