using Microsoft.EntityFrameworkCore;
using ModApi.Application.Abstractions;
using ModApi.Domain.Common;

namespace ModApi.Infrastructure.Persistence;

public class Repository<TEntity, TKey>(AppDbContext context) : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    protected AppDbContext Context { get; } = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default) =>
        await Shape(query).ToListAsync(cancellationToken);

    public Task<int> CountAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        CancellationToken cancellationToken = default) =>
        Shape(query).CountAsync(cancellationToken);

    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Remove(TEntity entity) => Set.Remove(entity);

    private IQueryable<TEntity> Shape(Func<IQueryable<TEntity>, IQueryable<TEntity>>? query)
    {
        var source = Set.AsNoTracking();
        return query is null ? source : query(source);
    }
}
