using System.Diagnostics.CodeAnalysis;
using ModApi.Api.Controllers;

namespace ModApi.Api.Conventions;

/// <summary>Helpers for recognising (subclasses of) <see cref="CrudController{TEntity, TKey}"/>.</summary>
public static class CrudControllerTypes
{
    /// <summary>
    /// Returns the entity type when <paramref name="controllerType"/> is, or derives from, a closed <c>CrudController&lt;,&gt;</c>.
    /// </summary>
    public static bool TryGetEntityType(Type controllerType, [NotNullWhen(true)] out Type? entityType)
    {
        ArgumentNullException.ThrowIfNull(controllerType);

        for (var type = controllerType; type is not null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && !type.ContainsGenericParameters && type.GetGenericTypeDefinition() == typeof(CrudController<,>))
            {
                entityType = type.GetGenericArguments()[0];
                return true;
            }
        }

        entityType = null;
        return false;
    }
}
