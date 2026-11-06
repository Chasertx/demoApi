using ModApi.Application.Exceptions;

namespace ModApi.Application.Querying;

/// <summary>
/// Parses query-string pairs of the form <c>property=value</c> or <c>property:operator=value</c>
/// into <see cref="FilterCriterion"/> instances.
/// </summary>
public static class FilterParser
{
    private static readonly Dictionary<string, FilterOperator> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eq"] = FilterOperator.Equal,
        ["ne"] = FilterOperator.NotEqual,
        ["gt"] = FilterOperator.GreaterThan,
        ["gte"] = FilterOperator.GreaterThanOrEqual,
        ["lt"] = FilterOperator.LessThan,
        ["lte"] = FilterOperator.LessThanOrEqual,
        ["contains"] = FilterOperator.Contains,
        ["startswith"] = FilterOperator.StartsWith,
        ["endswith"] = FilterOperator.EndsWith,
    };

    /// <summary>The operator tokens accepted after the colon.</summary>
    public static IReadOnlyCollection<string> SupportedOperators => Operators.Keys;

    /// <summary>Parses the given key/value pairs.</summary>
    /// <exception cref="RequestValidationException">A key is malformed or uses an unknown operator.</exception>
    public static IReadOnlyList<FilterCriterion> Parse(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        var errors = new ValidationErrorCollector();
        var result = new List<FilterCriterion>();

        foreach (var (key, value) in pairs)
        {
            var separator = key.IndexOf(':', StringComparison.Ordinal);
            var property = (separator < 0 ? key : key[..separator]).Trim();
            var operatorToken = separator < 0 ? "eq" : key[(separator + 1)..].Trim();

            if (property.Length == 0)
            {
                errors.Add(key, "A filter must name a property, e.g. 'name=value' or 'price:gte=10'.");
                continue;
            }

            if (!Operators.TryGetValue(operatorToken, out var op))
            {
                errors.Add(key, $"Unknown filter operator '{operatorToken}'. Supported operators: {string.Join(", ", Operators.Keys)}.");
                continue;
            }

            result.Add(new FilterCriterion(property, op, value));
        }

        errors.ThrowIfAny();
        return result;
    }
}
