namespace ModApi.Application.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors, StringComparer.OrdinalIgnoreCase);
    }

    public RequestValidationException(string key, params string[] messages)
        : this(new Dictionary<string, string[]> { [key] = messages })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
