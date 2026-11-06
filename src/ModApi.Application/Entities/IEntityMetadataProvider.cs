namespace ModApi.Application.Entities;

/// <summary>Supplies UI-oriented metadata about every exposed entity.</summary>
public interface IEntityMetadataProvider
{
    /// <summary>Metadata for every exposed entity, ordered by name.</summary>
    IReadOnlyList<EntityMetadata> GetAll();

    /// <summary>Metadata for the entity at <paramref name="route"/>, or <c>null</c>.</summary>
    EntityMetadata? Find(string route);
}
