using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using ModApi.Api.Controllers;
using ModApi.Application.Entities;

namespace ModApi.Api.Conventions;

public sealed class CrudControllerFeatureProvider(IEntityRegistry registry) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        // Entities that already have a hand-written CrudController subclass don't get a generic one.
        var handled = new HashSet<Type>();
        foreach (var controller in feature.Controllers)
        {
            if (CrudControllerTypes.TryGetEntityType(controller, out var entityType))
            {
                handled.Add(entityType);
            }
        }

        foreach (var entity in registry.Entities.Where(e => e.IsExposed && !handled.Contains(e.EntityType)))
        {
            var controllerType = typeof(CrudController<,>).MakeGenericType(entity.EntityType, entity.KeyType);
            feature.Controllers.Add(controllerType.GetTypeInfo());
        }
    }
}
