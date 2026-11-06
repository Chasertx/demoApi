namespace ModApi.Domain.Common;

public interface IEntity<TKey>
    where TKey : notnull
{
    TKey Id { get; set; }
}
