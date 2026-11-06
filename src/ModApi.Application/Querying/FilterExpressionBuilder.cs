using System.Linq.Expressions;
using System.Reflection;
using ModApi.Application.Exceptions;
using ModApi.Application.Reflection;

namespace ModApi.Application.Querying;

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

    public static Expression<Func<TEntity, bool>>? Build<TEntity>(IEnumerable<FilterCriterion> filters, string? search)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var properties = EntityProperties.For(typeof(TEntity));
        var errors = new ValidationErrorCollector();
        Expression? body = null;

        foreach (var filter in filters)
        {
            body = And(body, BuildFilter(parameter, properties, filter, errors));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            body = And(body, BuildSearch(parameter, properties, search.Trim(), errors));
        }

        errors.ThrowIfAny();
        return body is null ? null : Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression? And(Expression? left, Expression? right) =>
        left is null ? right : right is null ? left : Expression.AndAlso(left, right);

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

        var propertyType = property.PropertyType;
        if (!ValueParser.TryParse(filter.Value, propertyType, out var value))
        {
            errors.Add(filter.Property, $"'{filter.Value}' is not a valid value for '{property.Name}' ({TypeName(propertyType)}).");
            return null;
        }

        if (value is null && filter.Operator is not (FilterOperator.Equal or FilterOperator.NotEqual))
        {
            errors.Add(filter.Property, "Only the 'eq' and 'ne' operators can be compared with null.");
            return null;
        }

        var member = Expression.Property(parameter, property);

        switch (filter.Operator)
        {
            case FilterOperator.Equal:
                return Expression.Equal(member, Parameter(value, propertyType));

            case FilterOperator.NotEqual:
                return Expression.NotEqual(member, Parameter(value, propertyType));

            case FilterOperator.GreaterThan:
            case FilterOperator.GreaterThanOrEqual:
            case FilterOperator.LessThan:
            case FilterOperator.LessThanOrEqual:
                if (!OrderableTypes.Contains(Nullable.GetUnderlyingType(propertyType) ?? propertyType))
                {
                    errors.Add(filter.Property, $"Range operators are not supported for '{property.Name}' ({TypeName(propertyType)}).");
                    return null;
                }

                var right = Parameter(value, propertyType);
                return filter.Operator switch
                {
                    FilterOperator.GreaterThan => Expression.GreaterThan(member, right),
                    FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, right),
                    FilterOperator.LessThan => Expression.LessThan(member, right),
                    _ => Expression.LessThanOrEqual(member, right),
                };

            default:
                if (propertyType != typeof(string))
                {
                    errors.Add(filter.Property, $"Text operators only work on strings; '{property.Name}' is {TypeName(propertyType)}.");
                    return null;
                }

                var method = filter.Operator switch
                {
                    FilterOperator.Contains => ContainsMethod,
                    FilterOperator.StartsWith => StartsWithMethod,
                    _ => EndsWithMethod,
                };
                return TextMatch(member, method, ((string)value!).ToLowerInvariant());
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
            .Select(p => TextMatch(Expression.Property(parameter, p), ContainsMethod, term))
            .Aggregate(Expression.OrElse);
    }

    // Lower-casing both sides keeps matching case-insensitive on Postgres too, where LIKE is case-sensitive.
    private static Expression TextMatch(MemberExpression member, MethodInfo method, string term)
    {
        var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
        var match = Expression.Call(Expression.Call(member, ToLowerMethod), method, Parameter(term, typeof(string)));
        return Expression.AndAlso(notNull, match);
    }

    // EF Core turns a member access on a captured object into a SQL parameter; a plain constant would be inlined.
    private static MemberExpression Parameter(object? value, Type type)
    {
        var holder = Activator.CreateInstance(typeof(Box<>).MakeGenericType(type), value)!;
        return Expression.Property(Expression.Constant(holder), nameof(Box<object>.Value));
    }

    private static string TypeName(Type type) =>
        Nullable.GetUnderlyingType(type) is { } underlying ? $"{underlying.Name}?" : type.Name;

    private sealed class Box<T>(T value)
    {
        public T Value { get; } = value;
    }
}
