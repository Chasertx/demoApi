using System.Linq.Expressions;
using System.Reflection;
using ModApi.Application.Exceptions;
using ModApi.Application.Reflection;

namespace ModApi.Application.Querying;

/// <summary>A single sort instruction.</summary>
/// <param name="Property">The property to order by.</param>
/// <param name="Descending">Whether to sort in descending order.</param>
public sealed record SortDescriptor(PropertyInfo Property, bool Descending);

/// <summary>Parses sort expressions and applies ordering and paging to <see cref="IQueryable{T}"/>.</summary>
public static class SortBuilder
{
    /// <summary>
    /// Parses <c>"name,-price"</c> style sort expressions. A leading <c>-</c> means descending.
    /// </summary>
    /// <exception cref="RequestValidationException">A named property is unknown or not sortable.</exception>
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
            var descending = token.StartsWith('-');
            var name = token.TrimStart('-', '+');

            if (!properties.TryGetQueryable(name, out var property))
            {
                errors.Add("sort", $"'{name}' is not a sortable property of {typeof(TEntity).Name}.");
                continue;
            }

            if (result.All(existing => existing.Property != property))
            {
                result.Add(new SortDescriptor(property, descending));
            }
        }

        errors.ThrowIfAny();
        return result;
    }

    /// <summary>
    /// Applies the given ordering. <c>Id</c> is always appended as a final tie-breaker so paging is deterministic.
    /// </summary>
    public static IQueryable<TEntity> ApplySort<TEntity>(this IQueryable<TEntity> query, IReadOnlyList<SortDescriptor> sorts)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sorts);

        var effective = sorts.ToList();
        if (EntityProperties.For(typeof(TEntity)).TryGetQueryable("Id", out var id) && effective.All(s => s.Property != id))
        {
            effective.Add(new SortDescriptor(id, Descending: false));
        }

        var expression = query.Expression;
        for (var i = 0; i < effective.Count; i++)
        {
            var sort = effective[i];
            var methodName = (i == 0, sort.Descending) switch
            {
                (true, false) => nameof(Queryable.OrderBy),
                (true, true) => nameof(Queryable.OrderByDescending),
                (false, false) => nameof(Queryable.ThenBy),
                (false, true) => nameof(Queryable.ThenByDescending),
            };

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var keySelector = Expression.Lambda(Expression.Property(parameter, sort.Property), parameter);
            expression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(TEntity), sort.Property.PropertyType],
                expression,
                Expression.Quote(keySelector));
        }

        return query.Provider.CreateQuery<TEntity>(expression);
    }

    /// <summary>Applies 1-based paging.</summary>
    public static IQueryable<TEntity> ApplyPaging<TEntity>(this IQueryable<TEntity> query, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
