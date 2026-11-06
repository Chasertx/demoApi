using System.ComponentModel;

namespace ModApi.Domain.Common;

/// <summary>
/// Convenience base class providing an <c>Id</c> and audit timestamps.
/// Entities may also implement <see cref="IEntity{TKey}"/> directly (useful for scaffolded, partial classes).
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class EntityBase<TKey> : IEntity<TKey>, IAuditable
    where TKey : notnull
{
    /// <inheritdoc />
    public TKey Id { get; set; } = default!;

    /// <inheritdoc />
    [ReadOnly(true)]
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc />
    [ReadOnly(true)]
    public DateTime? UpdatedAtUtc { get; set; }
}
