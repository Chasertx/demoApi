using System.Diagnostics.CodeAnalysis;
using ModApi.Api.Controllers;

namespace ModApi.Api.Conventions;

public static class CrudControllerTypes
{
    public static bool TryGetEntityType(Type controllerType, [NotNullWhen(true)] out Type? entityType)
    {
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
