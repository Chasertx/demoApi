using Microsoft.EntityFrameworkCore;
using ModApi.Application.Abstractions;
using ModApi.Domain.Common;

namespace ModApi.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IRepository{TEntity, TKey}"/> for any registered entity.</summary>
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    public Repository(AppDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected AppDbContext Context { get; }

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default) =>
        await Shape(query).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default) =>
        Shape(query).CountAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }

    private IQueryable<TEntity> Shape(Func<IQueryable<TEntity>, IQueryable<TEntity>>? query)
    {
        IQueryable<TEntity> source = Set.AsNoTracking();
        return query is null ? source : query(source);
    }
}
