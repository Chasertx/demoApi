using Microsoft.AspNetCore.Diagnostics;

namespace ModApi.Api.ErrorHandling;

/// <summary>
/// Converts unhandled exceptions into consistent problem-details responses (ASP.NET Core 8 <see cref="IExceptionHandler"/>).
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var problem = ExceptionProblemDetailsMapper.Map(exception, _environment.IsDevelopment());
        var status = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandled(status, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}.")]
    private partial void LogUnhandled(Exception exception, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request failed with status {Status}: {Reason}")]
    private partial void LogHandled(int status, string reason);
}
