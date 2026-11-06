using System.Text.Json;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;
using ModApi.Application.Querying;
using ModApi.Domain.Common;

namespace ModApi.Application.Services;

public class CrudService<TEntity, TKey>(
    IRepository<TEntity, TKey> repository,
    IUnitOfWork unitOfWork,
    IEntityMapper mapper) : ICrudService<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    private static readonly string EntityName = typeof(TEntity).Name;

    // The database (or EF for Guids) generates these, so whatever the client sends is ignored on create.
    private static readonly bool KeyIsGenerated =
        typeof(TKey) == typeof(int) || typeof(TKey) == typeof(long) || typeof(TKey) == typeof(short) || typeof(TKey) == typeof(Guid);

    protected IRepository<TEntity, TKey> Repository { get; } = repository;

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;

    protected IEntityMapper Mapper { get; } = mapper;

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(QueryParameters parameters, CancellationToken cancellationToken = default)
    {
        ValidatePaging(parameters);

        // Build these up front so bad input is rejected before we hit the database.
        var predicate = FilterExpressionBuilder.Build<TEntity>(parameters.Filters, parameters.Search);
        var sorts = SortBuilder.Parse<TEntity>(parameters.Sort);

        IQueryable<TEntity> Filter(IQueryable<TEntity> query) => predicate is null ? query : query.Where(predicate);

        var total = await Repository.CountAsync(Filter, cancellationToken);
        if (total == 0)
        {
            return new PagedResult<TEntity>([], parameters.Page, parameters.PageSize, 0);
        }

        var items = await Repository.ListAsync(
            query => Filter(query).ApplySort(sorts).ApplyPaging(parameters.Page, parameters.PageSize),
            cancellationToken);

        return new PagedResult<TEntity>(items, parameters.Page, parameters.PageSize, total);
    }

    public virtual Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        FindOrThrowAsync(id, cancellationToken);

    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (KeyIsGenerated)
        {
            entity.Id = default!;
        }

        await Repository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default)
    {
        var comparer = EqualityComparer<TKey>.Default;
        if (!comparer.Equals(entity.Id, default!) && !comparer.Equals(entity.Id, id))
        {
            throw new RequestValidationException("id", "The id in the request body does not match the id in the URL.");
        }

        var existing = await FindOrThrowAsync(id, cancellationToken);
        Mapper.CopyValues(entity, existing);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public virtual async Task<TEntity> PatchAsync(TKey id, IReadOnlyDictionary<string, JsonElement> patch, CancellationToken cancellationToken = default)
    {
        var existing = await FindOrThrowAsync(id, cancellationToken);
        Mapper.ApplyPatch(existing, patch);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var existing = await FindOrThrowAsync(id, cancellationToken);
        Repository.Remove(existing);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    protected async Task<TEntity> FindOrThrowAsync(TKey id, CancellationToken cancellationToken) =>
        await Repository.FindAsync(id, cancellationToken) ?? throw new NotFoundException(EntityName, id);

    private static void ValidatePaging(QueryParameters parameters)
    {
        var errors = new ValidationErrorCollector();

        if (parameters.Page < 1)
        {
            errors.Add("page", "Page must be 1 or greater.");
        }

        if (parameters.PageSize is < 1 or > QueryParameters.MaxPageSize)
        {
            errors.Add("pageSize", $"Page size must be between 1 and {QueryParameters.MaxPageSize}.");
        }

        errors.ThrowIfAny();
    }
}
