namespace ModApi.Domain.Common;

/// <summary>
/// Entities implementing this interface get their audit timestamps maintained automatically
/// when changes are saved. Clients can never overwrite these values through the API.
/// </summary>
public interface IAuditable
{
    /// <summary>When the record was created (UTC).</summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>When the record was last modified (UTC), or <c>null</c> if it has never been modified.</summary>
    DateTime? UpdatedAtUtc { get; set; }
}
