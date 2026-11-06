using System.ComponentModel;

namespace ModApi.Domain.Common;

public abstract class EntityBase<TKey> : IEntity<TKey>, IAuditable
    where TKey : notnull
{
    public TKey Id { get; set; } = default!;

    [ReadOnly(true)]
    public DateTime CreatedAtUtc { get; set; }

    [ReadOnly(true)]
    public DateTime? UpdatedAtUtc { get; set; }
}
