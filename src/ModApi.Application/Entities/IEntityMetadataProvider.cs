namespace ModApi.Application.Entities;

public interface IEntityMetadataProvider
{
    IReadOnlyList<EntityMetadata> GetAll();

    EntityMetadata? Find(string route);
}
