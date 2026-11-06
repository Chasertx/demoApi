using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using ModApi.Api.Controllers;
using ModApi.Application.Entities;

namespace ModApi.Api.Conventions;

/// <summary>
/// Adds a closed <see cref="CrudController{TEntity, TKey}"/> to MVC for every exposed entity that does not
/// already have a hand-written controller deriving from <c>CrudController&lt;TEntity, TKey&gt;</c>.
/// </summary>
public sealed class CrudControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly IEntityRegistry _registry;

    public CrudControllerFeatureProvider(IEntityRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        var handled = new HashSet<Type>();
        foreach (var controller in feature.Controllers)
        {
            if (CrudControllerTypes.TryGetEntityType(controller, out var entityType))
            {
                handled.Add(entityType);
            }
        }

        foreach (var descriptor in _registry.Entities.Where(d => d.IsExposed && !handled.Contains(d.EntityType)))
        {
            feature.Controllers.Add(
                typeof(CrudController<,>).MakeGenericType(descriptor.EntityType, descriptor.KeyType).GetTypeInfo());
        }
    }
}
