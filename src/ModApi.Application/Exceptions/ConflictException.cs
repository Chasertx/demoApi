namespace ModApi.Application.Exceptions;

public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
