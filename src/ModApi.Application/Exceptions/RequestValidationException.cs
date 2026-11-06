namespace ModApi.Application.Exceptions;

/// <summary>
/// Thrown when client input is invalid (unknown filter, bad value, failed validation...).
/// Mapped to HTTP 400 with an RFC 9457 validation problem body.
/// </summary>
public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = new Dictionary<string, string[]>(errors, StringComparer.OrdinalIgnoreCase);
    }

    public RequestValidationException(string key, params string[] messages)
        : this(new Dictionary<string, string[]> { [key] = messages })
    {
    }

    /// <summary>Error messages keyed by the offending field or query parameter.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
