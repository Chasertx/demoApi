namespace ModApi.Application.Querying;

/// <summary>Paging, sorting, searching and filtering options for list queries.</summary>
public sealed record QueryParameters
{
    /// <summary>Default number of items per page.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>Largest page size a client may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Items per page (1 – <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>Comma-separated property names; prefix with <c>-</c> for descending, e.g. <c>name,-createdAtUtc</c>.</summary>
    public string? Sort { get; init; }

    /// <summary>Case-insensitive text matched against every string property.</summary>
    public string? Search { get; init; }

    /// <summary>Property filters combined with logical AND.</summary>
    public IReadOnlyList<FilterCriterion> Filters { get; init; } = [];
}
