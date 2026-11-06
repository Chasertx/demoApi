namespace ModApi.Application.Querying;

/// <summary>Comparison operators supported by query-string filters (<c>?property:operator=value</c>).</summary>
public enum FilterOperator
{
    /// <summary><c>eq</c> (default) – equal.</summary>
    Equal,

    /// <summary><c>ne</c> – not equal.</summary>
    NotEqual,

    /// <summary><c>gt</c> – greater than.</summary>
    GreaterThan,

    /// <summary><c>gte</c> – greater than or equal.</summary>
    GreaterThanOrEqual,

    /// <summary><c>lt</c> – less than.</summary>
    LessThan,

    /// <summary><c>lte</c> – less than or equal.</summary>
    LessThanOrEqual,

    /// <summary><c>contains</c> – case-insensitive substring (strings only).</summary>
    Contains,

    /// <summary><c>startswith</c> – case-insensitive prefix (strings only).</summary>
    StartsWith,

    /// <summary><c>endswith</c> – case-insensitive suffix (strings only).</summary>
    EndsWith,
}
