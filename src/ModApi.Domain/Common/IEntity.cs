namespace ModApi.Domain.Common;

/// <summary>
/// Marks a class as a persisted entity with a primary key named <c>Id</c>.
/// Every non-abstract type implementing this interface in a registered assembly is
/// automatically added to the EF Core model and exposed through the generic CRUD API.
/// </summary>
/// <typeparam name="TKey">The primary key type (for example <see cref="int"/>, <see cref="long"/>, <see cref="Guid"/> or <see cref="string"/>).</typeparam>
public interface IEntity<TKey>
    where TKey : notnull
{
    /// <summary>The primary key.</summary>
    TKey Id { get; set; }
}
