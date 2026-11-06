using ModApi.Application.Exceptions;

namespace ModApi.Application.Querying;

// Turns "name=x" and "price:gte=10" style query string pairs into filter criteria.
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

    public static IReadOnlyCollection<string> SupportedOperators => Operators.Keys;

    public static IReadOnlyList<FilterCriterion> Parse(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        var errors = new ValidationErrorCollector();
        var result = new List<FilterCriterion>();

        foreach (var (key, value) in pairs)
        {
            var separator = key.IndexOf(':');
            var property = (separator < 0 ? key : key[..separator]).Trim();
            var token = separator < 0 ? "eq" : key[(separator + 1)..].Trim();

            if (property.Length == 0)
            {
                errors.Add(key, "A filter must name a property, e.g. 'name=value' or 'price:gte=10'.");
                continue;
            }

            if (!Operators.TryGetValue(token, out var op))
            {
                errors.Add(key, $"Unknown filter operator '{token}'. Supported operators: {string.Join(", ", Operators.Keys)}.");
                continue;
            }

            result.Add(new FilterCriterion(property, op, value));
        }

        errors.ThrowIfAny();
        return result;
    }
}
