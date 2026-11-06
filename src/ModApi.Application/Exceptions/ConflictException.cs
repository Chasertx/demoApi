namespace ModApi.Application.Exceptions;

/// <summary>
/// Thrown when a change conflicts with existing data (unique index, foreign key, concurrency).
/// Mapped to HTTP 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
