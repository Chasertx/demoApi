using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ModApi.Application.Entities;

/// <summary>
/// The single source of truth for which entities exist. Shared by the EF Core model builder
/// (Infrastructure) and the controller generator (Api) so both always agree.
/// </summary>
public interface IEntityRegistry
{
    /// <summary>All discovered entities, ordered by name.</summary>
    IReadOnlyList<EntityDescriptor> Entities { get; }

    /// <summary>Assemblies the entities came from (also scanned for EF Core type configurations).</summary>
    IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>A stable string identifying the entity set; used as part of the EF Core model cache key.</summary>
    string Signature { get; }

    /// <summary>Looks up an entity by CLR type.</summary>
    bool TryGetByType(Type entityType, [NotNullWhen(true)] out EntityDescriptor? descriptor);

    /// <summary>Looks up an entity by route segment (case-insensitive).</summary>
    bool TryGetByRoute(string route, [NotNullWhen(true)] out EntityDescriptor? descriptor);
}
