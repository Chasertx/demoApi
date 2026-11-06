namespace ModApi.IntegrationTests.Infrastructure;

// Client-side contracts, deliberately independent of the server's domain types (like a real frontend).

public sealed record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage);

public sealed record CategoryDto(int Id, string Name, string? Description, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record ProductDto(
    int Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    string Status,
    int? CategoryId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string? PhoneNumber, bool IsActive);

public sealed record ProblemResponse(int Status, string? Title, string? Detail, Dictionary<string, string[]>? Errors);
