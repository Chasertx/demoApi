using System.Text.Json;

namespace ModApi.Application.Abstractions;

/// <summary>Copies client-supplied values onto tracked entities while protecting keys and audit fields.</summary>
public interface IEntityMapper
{
    /// <summary>Copies every writable scalar property from <paramref name="source"/> to <paramref name="target"/>.</summary>
    void CopyValues<TEntity>(TEntity source, TEntity target)
        where TEntity : class;

    /// <summary>
    /// Applies the supplied JSON property values to <paramref name="target"/> and validates the result.
    /// </summary>
    /// <exception cref="Exceptions.RequestValidationException">A property is unknown, read-only, or has an invalid value.</exception>
    void ApplyPatch<TEntity>(TEntity target, IReadOnlyDictionary<string, JsonElement> patch)
        where TEntity : class;
}
