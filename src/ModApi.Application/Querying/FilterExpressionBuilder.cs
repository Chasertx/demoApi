using System.Linq.Expressions;
using System.Reflection;
using ModApi.Application.Exceptions;
using ModApi.Application.Reflection;

namespace ModApi.Application.Querying;

/// <summary>
/// Translates <see cref="FilterCriterion"/> instances and a free-text search term into a LINQ predicate.
/// The resulting expression tree contains only operations every EF Core relational provider can translate
/// (comparisons, <c>ToLower</c>, <c>Contains</c>, <c>StartsWith</c>, <c>EndsWith</c>), and every value is
/// captured as a closure so EF Core sends it as a SQL parameter – never concatenated into SQL.
/// </summary>
public static class FilterExpressionBuilder
{
    private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

    private static readonly HashSet<Type> OrderableTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
    ];

    /// <summary>Builds a predicate, or returns <c>null</c> when there is nothing to filter.</summary>
    /// <exception cref="RequestValidationException">Any filter or the search term is invalid; all problems are reported together.</exception>
    public static Expression<Func<TEntity, bool>>? Build<TEntity>(IEnumerable<FilterCriterion> filters, string? search)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var properties = EntityProperties.For(typeof(TEntity));
        var errors = new ValidationErrorCollector();
        Expression? body = null;

        foreach (var filter in filters)
        {
            var expression = BuildFilter(parameter, properties, filter, errors);
            if (expression is not null)
            {
                body = body is null ? expression : Expression.AndAlso(body, expression);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var expression = BuildSearch(parameter, properties, search.Trim(), errors);
            if (expression is not null)
            {
                body = body is null ? expression : Expression.AndAlso(body, expression);
            }
        }

        errors.ThrowIfAny();
        return body is null ? null : Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression? BuildFilter(
        ParameterExpression parameter,
        EntityPropertySet properties,
        FilterCriterion filter,
        ValidationErrorCollector errors)
    {
        if (!properties.TryGetQueryable(filter.Property, out var property))
        {
            errors.Add(filter.Property, $"'{filter.Property}' is not a filterable property of {properties.EntityType.Name}.");
            return null;
        }

        if (!ValueParser.TryParse(filter.Value, property.PropertyType, out var value))
        {
            errors.Add(filter.Property, $"'{filter.Value}' is not a valid value for '{property.Name}' ({DescribeType(property.PropertyType)}).");
            return null;
        }

        if (value is null && filter.Operator is not (FilterOperator.Equal or FilterOperator.NotEqual))
        {
            errors.Add(filter.Property, "Only the 'eq' and 'ne' operators can be compared with null.");
            return null;
        }

        var member = Expression.Property(parameter, property);
        var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        switch (filter.Operator)
        {
            case FilterOperator.Equal:
                return Expression.Equal(member, Parameterize(value, property.PropertyType));
            case FilterOperator.NotEqual:
                return Expression.NotEqual(member, Parameterize(value, property.PropertyType));
            case FilterOperator.GreaterThan:
            case FilterOperator.GreaterThanOrEqual:
            case FilterOperator.LessThan:
            case FilterOperator.LessThanOrEqual:
                if (!OrderableTypes.Contains(underlying))
                {
                    errors.Add(filter.Property, $"Range operators are not supported for '{property.Name}' ({DescribeType(property.PropertyType)}).");
                    return null;
                }

                var right = Parameterize(value, property.PropertyType);
                return filter.Operator switch
                {
                    FilterOperator.GreaterThan => Expression.GreaterThan(member, right),
                    FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, right),
                    FilterOperator.LessThan => Expression.LessThan(member, right),
                    _ => Expression.LessThanOrEqual(member, right),
                };
            case FilterOperator.Contains:
            case FilterOperator.StartsWith:
            case FilterOperator.EndsWith:
                if (property.PropertyType != typeof(string))
                {
                    errors.Add(filter.Property, $"Text operators can only be used on string properties; '{property.Name}' is {DescribeType(property.PropertyType)}.");
                    return null;
                }

                var method = filter.Operator switch
                {
                    FilterOperator.Contains => ContainsMethod,
                    FilterOperator.StartsWith => StartsWithMethod,
                    _ => EndsWithMethod,
                };
                return BuildTextMatch(member, method, ((string)value!).ToLowerInvariant());
            default:
                errors.Add(filter.Property, $"Operator '{filter.Operator}' is not supported.");
                return null;
        }
    }

    private static Expression? BuildSearch(
        ParameterExpression parameter,
        EntityPropertySet properties,
        string search,
        ValidationErrorCollector errors)
    {
        if (properties.Text.Count == 0)
        {
            errors.Add("search", $"{properties.EntityType.Name} has no text properties to search.");
            return null;
        }

        var term = search.ToLowerInvariant();
        return properties.Text
            .Select(p => BuildTextMatch(Expression.Property(parameter, p), ContainsMethod, term))
            .Aggregate(Expression.OrElse);
    }

    /// <summary><c>member != null &amp;&amp; member.ToLower().Method(term)</c> – case-insensitive on every provider.</summary>
    private static Expression BuildTextMatch(MemberExpression member, MethodInfo method, string loweredTerm)
    {
        var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
        var lowered = Expression.Call(member, ToLowerMethod);
        var match = Expression.Call(lowered, method, Parameterize(loweredTerm, typeof(string)));
        return Expression.AndAlso(notNull, match);
    }

    /// <summary>Wraps a value in a closure-like holder so EF Core emits a SQL parameter instead of a literal.</summary>
    private static MemberExpression Parameterize(object? value, Type type)
    {
        var holder = Activator.CreateInstance(typeof(ValueHolder<>).MakeGenericType(type), value)!;
        return Expression.Property(Expression.Constant(holder), nameof(ValueHolder<object>.Value));
    }

    private static string DescribeType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        return underlying is null ? type.Name : $"{underlying.Name}?";
    }

    private sealed class ValueHolder<T>(T value)
    {
        public T Value { get; } = value;
    }
}
