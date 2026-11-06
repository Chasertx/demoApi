using System.Text.Json;

namespace ModApi.Application.Abstractions;

public interface IEntityMapper
{
    void CopyValues<TEntity>(TEntity source, TEntity target)
        where TEntity : class;

    void ApplyPatch<TEntity>(TEntity target, IReadOnlyDictionary<string, JsonElement> patch)
        where TEntity : class;
}
