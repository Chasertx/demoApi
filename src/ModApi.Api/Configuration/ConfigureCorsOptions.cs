using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace ModApi.Api.Configuration;

/// <summary>Builds the default CORS policy from <see cref="CorsSettings"/>.</summary>
public sealed class ConfigureCorsOptions : IConfigureOptions<CorsOptions>
{
    private readonly CorsSettings _settings;

    public ConfigureCorsOptions(IOptions<CorsSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public void Configure(CorsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var origins = _settings.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .ToArray();
        var allowAny = origins.Contains("*");

        options.AddDefaultPolicy(policy =>
        {
            if (allowAny)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(origins);
                if (_settings.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            }

            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Location");
        });
    }
}
