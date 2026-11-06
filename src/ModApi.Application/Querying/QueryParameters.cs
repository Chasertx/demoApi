namespace ModApi.Application.Querying;

public sealed record QueryParameters
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    // e.g. "name,-createdAtUtc"
    public string? Sort { get; init; }

    public string? Search { get; init; }

    public IReadOnlyList<FilterCriterion> Filters { get; init; } = [];
}
