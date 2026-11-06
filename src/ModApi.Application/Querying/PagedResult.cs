namespace ModApi.Application.Querying;

/// <summary>A page of results plus the information a client needs to render pagination.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">Items on this page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="TotalCount">Total number of matching items across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage => Page < TotalPages;
}
