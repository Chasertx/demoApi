using Microsoft.AspNetCore.Mvc;
using ModApi.Application.Exceptions;

namespace ModApi.Api.ErrorHandling;

/// <summary>Maps exceptions to RFC 9457 problem details. Pure and side-effect free for easy testing.</summary>
public static class ExceptionProblemDetailsMapper
{
    /// <summary>Non-standard status used when the client aborted the request.</summary>
    public const int ClientClosedRequest = 499;

    /// <summary>Creates the problem details for <paramref name="exception"/>.</summary>
    /// <param name="exception">The exception to map.</param>
    /// <param name="includeExceptionDetails">Include the stack trace for unexpected errors (development only).</param>
    public static ProblemDetails Map(Exception exception, bool includeExceptionDetails)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            RequestValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            },
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = notFound.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Detail = conflict.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Bad request.",
                Detail = badRequest.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            },
            OperationCanceledException => new ProblemDetails
            {
                Status = ClientClosedRequest,
                Title = "The request was cancelled.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = includeExceptionDetails ? exception.ToString() : null,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            },
        };
    }
}
