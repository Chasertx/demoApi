using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ModApi.Application.Entities;

public interface IEntityRegistry
{
    IReadOnlyList<EntityDescriptor> Entities { get; }

    IReadOnlyList<Assembly> Assemblies { get; }

    // Used in the EF model cache key so different entity sets never share a cached model.
    string Signature { get; }

    bool TryGetByType(Type entityType, [NotNullWhen(true)] out EntityDescriptor? descriptor);

    bool TryGetByRoute(string route, [NotNullWhen(true)] out EntityDescriptor? descriptor);
}
