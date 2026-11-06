using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ModApi.Api.Configuration;

/// <summary>Configures a global fixed-window limiter partitioned by client IP from <see cref="RateLimitingSettings"/>.</summary>
public sealed class ConfigureRateLimiterOptions : IConfigureOptions<RateLimiterOptions>
{
    private readonly RateLimitingSettings _settings;

    public ConfigureRateLimiterOptions(IOptions<RateLimitingSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public void Configure(RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (!_settings.Enabled)
            {
                return RateLimitPartition.GetNoLimiter("unlimited");
            }

            var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = _settings.PermitLimit,
                Window = TimeSpan.FromSeconds(_settings.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
        });
    }
}
