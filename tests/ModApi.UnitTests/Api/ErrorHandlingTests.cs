using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ModApi.Api.ErrorHandling;
using ModApi.Application.Exceptions;

namespace ModApi.UnitTests.Api;

public class ErrorHandlingTests
{
    [Fact]
    public void Validation_errors_map_to_400_with_field_errors()
    {
        var problem = ExceptionProblemDetailsMapper.Map(new RequestValidationException("name", "required"), false);

        var validation = Assert.IsType<ValidationProblemDetails>(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, validation.Status);
        Assert.Equal(["required"], validation.Errors["name"]);
    }

    [Fact]
    public void Not_found_maps_to_404()
    {
        var problem = ExceptionProblemDetailsMapper.Map(new NotFoundException("Widget", 7), false);

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Contains("'7'", problem.Detail);
    }

    [Fact]
    public void Conflicts_map_to_409()
    {
        var problem = ExceptionProblemDetailsMapper.Map(new ConflictException("duplicate"), false);

        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("duplicate", problem.Detail);
    }

    [Fact]
    public void Bad_http_requests_keep_their_status()
    {
        var problem = ExceptionProblemDetailsMapper.Map(new BadHttpRequestException("too large", StatusCodes.Status413PayloadTooLarge), false);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, problem.Status);
    }

    [Fact]
    public void Cancellation_maps_to_499()
    {
        var problem = ExceptionProblemDetailsMapper.Map(new OperationCanceledException(), false);

        Assert.Equal(ExceptionProblemDetailsMapper.ClientClosedRequest, problem.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unexpected_errors_map_to_500_and_only_reveal_details_when_allowed(bool includeDetails)
    {
        var problem = ExceptionProblemDetailsMapper.Map(new InvalidOperationException("secret"), includeDetails);

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal(includeDetails, problem.Detail?.Contains("secret") ?? false);
    }

    [Fact]
    public void Mapper_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => ExceptionProblemDetailsMapper.Map(null!, false));
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public async Task Handler_writes_problem_details_with_the_mapped_status(string environment, bool expectDetails)
    {
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        ProblemDetailsContext? written = null;
        problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(c => written = c)).Returns(true);
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environment);
        var handler = new GlobalExceptionHandler(problemDetailsService, hostEnvironment, NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.NotNull(written);
        Assert.Same(httpContext, written.HttpContext);
        Assert.Equal(expectDetails, written.ProblemDetails.Detail is not null);
    }

    [Fact]
    public async Task Handler_sets_client_error_statuses()
    {
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        problemDetailsService.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(true);
        var handler = new GlobalExceptionHandler(problemDetailsService, Substitute.For<IHostEnvironment>(), NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        await handler.TryHandleAsync(httpContext, new NotFoundException("Widget", 1), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public void Handler_validates_arguments()
    {
        var service = Substitute.For<IProblemDetailsService>();
        var environment = Substitute.For<IHostEnvironment>();
        var logger = NullLogger<GlobalExceptionHandler>.Instance;

        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!, environment, logger));
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(service, null!, logger));
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(service, environment, null!));
    }
}
