using Microsoft.AspNetCore.Mvc;
using ModApi.Application.Exceptions;

namespace ModApi.Api.ErrorHandling;

public static class ExceptionProblemDetailsMapper
{
    // nginx's convention for "client went away"
    public const int ClientClosedRequest = 499;

    public static ProblemDetails Map(Exception exception, bool includeExceptionDetails) => exception switch
    {
        RequestValidationException validation => new ValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
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
