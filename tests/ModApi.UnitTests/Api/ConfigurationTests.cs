using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ModApi.Api.Configuration;

namespace ModApi.UnitTests.Api;

public class ConfigurationTests
{
    private static CorsPolicy BuildCorsPolicy(CorsSettings settings)
    {
        var options = new CorsOptions();
        new ConfigureCorsOptions(Options.Create(settings)).Configure(options);
        return options.GetPolicy(options.DefaultPolicyName)!;
    }

    [Fact]
    public void Cors_allows_only_configured_origins()
    {
        var policy = BuildCorsPolicy(new CorsSettings { AllowedOrigins = ["https://app.example.com/", " ", "http://localhost:5173"] });

        Assert.Equal(["https://app.example.com", "http://localhost:5173"], policy.Origins);
        Assert.False(policy.AllowAnyOrigin);
        Assert.False(policy.SupportsCredentials);
        Assert.True(policy.AllowAnyHeader);
        Assert.True(policy.AllowAnyMethod);
        Assert.Contains("Location", policy.ExposedHeaders);
    }

    [Fact]
    public void Cors_supports_credentials_for_explicit_origins()
    {
        var policy = BuildCorsPolicy(new CorsSettings { AllowedOrigins = ["https://app.example.com"], AllowCredentials = true });

        Assert.True(policy.SupportsCredentials);
    }

    [Fact]
    public void Cors_wildcard_allows_any_origin_without_credentials()
    {
        var policy = BuildCorsPolicy(new CorsSettings { AllowedOrigins = ["*"], AllowCredentials = true });

        Assert.True(policy.AllowAnyOrigin);
        Assert.False(policy.SupportsCredentials);
    }

    [Fact]
    public void Cors_with_no_origins_allows_nobody()
    {
        var policy = BuildCorsPolicy(new CorsSettings());

        Assert.Empty(policy.Origins);
        Assert.False(policy.AllowAnyOrigin);
    }

    [Fact]
    public void Rate_limiter_rejects_requests_over_the_limit_per_client()
    {
        var options = new RateLimiterOptions();
        new ConfigureRateLimiterOptions(Options.Create(new RateLimitingSettings { PermitLimit = 2, WindowSeconds = 60 })).Configure(options);
        var client = new DefaultHttpContext();
        client.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        var otherClient = new DefaultHttpContext();
        otherClient.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.2");

        Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
        Assert.True(options.GlobalLimiter!.AttemptAcquire(client).IsAcquired);
        Assert.True(options.GlobalLimiter.AttemptAcquire(client).IsAcquired);
        Assert.False(options.GlobalLimiter.AttemptAcquire(client).IsAcquired);
        Assert.True(options.GlobalLimiter.AttemptAcquire(otherClient).IsAcquired);
    }

    [Fact]
    public void Disabled_rate_limiter_never_rejects()
    {
        var options = new RateLimiterOptions();
        new ConfigureRateLimiterOptions(Options.Create(new RateLimitingSettings { Enabled = false, PermitLimit = 1 })).Configure(options);
        var context = new DefaultHttpContext();

        for (var i = 0; i < 10; i++)
        {
            using RateLimitLease lease = options.GlobalLimiter!.AttemptAcquire(context);
            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public void Security_without_authentication_registers_permissive_defaults()
    {
        var services = new ServiceCollection().AddLogging();

        services.AddApiSecurity(new AuthenticationSettings { Enabled = false });

        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>().Value;
        Assert.Null(authorization.FallbackPolicy);
    }

    [Fact]
    public void Security_with_authentication_requires_an_authenticated_user_by_default()
    {
        var services = new ServiceCollection().AddLogging();

        services.AddApiSecurity(new AuthenticationSettings { Enabled = true, Authority = "https://login.example.com", Audience = "api" });

        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>().Value;
        Assert.NotNull(authorization.FallbackPolicy);
        var jwt = provider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>().Get("Bearer");
        Assert.Equal("https://login.example.com", jwt.Authority);
        Assert.Equal("api", jwt.Audience);
    }

    [Fact]
    public void Security_with_authentication_requires_an_authority()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddApiSecurity(new AuthenticationSettings { Enabled = true }));
    }

    [Fact]
    public async Task Health_report_is_written_as_json()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(HealthStatus.Healthy, "ok", TimeSpan.FromMilliseconds(12.5), null, null),
            },
            TimeSpan.FromMilliseconds(15));

        await HealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        var check = json.RootElement.GetProperty("checks")[0];
        Assert.Equal("database", check.GetProperty("name").GetString());
        Assert.Equal(12.5, check.GetProperty("durationMs").GetDouble());
    }

    [Fact]
    public void Settings_have_safe_defaults()
    {
        Assert.Empty(new CorsSettings().AllowedOrigins);
        Assert.True(new RateLimitingSettings().Enabled);
        Assert.False(new AuthenticationSettings().Enabled);
        Assert.True(new AuthenticationSettings().RequireHttpsMetadata);
        Assert.False(new SwaggerSettings().Enabled);
        Assert.True(new HostingSettings().UseHttpsRedirection);
    }
}
