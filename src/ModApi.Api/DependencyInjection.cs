using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ModApi.Api.Configuration;
using ModApi.Api.Conventions;
using ModApi.Api.ErrorHandling;
using ModApi.Application.Entities;
using InfrastructureRegistration = ModApi.Infrastructure.DependencyInjection;

namespace ModApi.Api;

/// <summary>Registers and wires the presentation (HTTP) layer.</summary>
public static class DependencyInjection
{
    /// <summary>Registers controllers (including generated CRUD controllers), Swagger, CORS, rate limiting, security and error handling.</summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration, IEntityRegistry entityRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(entityRegistry);

        services.AddOptions<CorsSettings>().Bind(configuration.GetSection(CorsSettings.SectionName));
        services.AddOptions<RateLimitingSettings>()
            .Bind(configuration.GetSection(RateLimitingSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SwaggerSettings>().Bind(configuration.GetSection(SwaggerSettings.SectionName));
        services.AddOptions<HostingSettings>().Bind(configuration.GetSection(HostingSettings.SectionName));

        services
            .AddControllers(options => options.Conventions.Add(new CrudControllerRouteConvention(entityRegistry)))
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new CrudControllerFeatureProvider(entityRegistry)))
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCors();
        services.AddSingleton<IConfigureOptions<CorsOptions>, ConfigureCorsOptions>();
        services.AddRateLimiter(_ => { });
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, ConfigureRateLimiterOptions>();

        var authentication = configuration.GetSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>() ?? new AuthenticationSettings();
        services.AddApiSecurity(authentication);
        services.AddSwaggerDocumentation(authentication);

        // Behind a reverse proxy / cloud ingress, honour X-Forwarded-For and X-Forwarded-Proto.
        // The container is only reachable through the ingress, so the proxy's address is not pinned.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    /// <summary>Builds the HTTP middleware pipeline and maps endpoints.</summary>
    public static WebApplication UsePresentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var hosting = app.Services.GetRequiredService<IOptions<HostingSettings>>().Value;
        var swagger = app.Services.GetRequiredService<IOptions<SwaggerSettings>>().Value;

        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        if (hosting.UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        if (swagger.Enabled)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/{SwaggerExtensions.DocumentName}/swagger.json", "ModApi v1");
                options.DocumentTitle = "ModApi – Swagger";
                options.DisplayRequestDuration();
                options.EnableTryItOutByDefault();
            });
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription().AllowAnonymous();
        }

        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Liveness: the process is up. Readiness: dependencies (database) are reachable.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthCheckResponseWriter.WriteAsync,
        }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(InfrastructureRegistration.ReadinessTag),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync,
        }).AllowAnonymous();

        return app;
    }
}
