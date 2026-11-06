using System.Linq.Expressions;
using System.Reflection;
using ModApi.Application.Exceptions;
using ModApi.Application.Reflection;

namespace ModApi.Application.Querying;

public sealed record SortDescriptor(PropertyInfo Property, bool Descending);

public static class SortBuilder
{
    // "name,-price" -> name ascending, then price descending
    public static IReadOnlyList<SortDescriptor> Parse<TEntity>(string? sort)
    {
        var result = new List<SortDescriptor>();
        if (string.IsNullOrWhiteSpace(sort))
        {
            return result;
        }

        var properties = EntityProperties.For(typeof(TEntity));
        var errors = new ValidationErrorCollector();

        foreach (var token in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = token.TrimStart('-', '+');

            if (!properties.TryGetQueryable(name, out var property))
            {
                errors.Add("sort", $"'{name}' is not a sortable property of {typeof(TEntity).Name}.");
                continue;
            }

            if (result.All(s => s.Property != property))
            {
                result.Add(new SortDescriptor(property, token.StartsWith('-')));
            }
        }

        errors.ThrowIfAny();
        return result;
    }

    public static IQueryable<TEntity> ApplySort<TEntity>(this IQueryable<TEntity> query, IReadOnlyList<SortDescriptor> sorts)
    {
        var effective = sorts.ToList();

        // Always finish with Id so paging is stable.
        if (EntityProperties.For(typeof(TEntity)).TryGetQueryable("Id", out var id) && effective.All(s => s.Property != id))
        {
            effective.Add(new SortDescriptor(id, Descending: false));
        }

        var expression = query.Expression;
        for (var i = 0; i < effective.Count; i++)
        {
            var (property, descending) = effective[i];
            var method = i == 0
                ? descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy)
                : descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy);

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var keySelector = Expression.Lambda(Expression.Property(parameter, property), parameter);

            expression = Expression.Call(
                typeof(Queryable),
                method,
                [typeof(TEntity), property.PropertyType],
                expression,
                Expression.Quote(keySelector));
        }

        return query.Provider.CreateQuery<TEntity>(expression);
    }

    public static IQueryable<TEntity> ApplyPaging<TEntity>(this IQueryable<TEntity> query, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
