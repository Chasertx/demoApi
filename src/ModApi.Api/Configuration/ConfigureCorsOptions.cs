using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace ModApi.Api.Configuration;

public sealed class ConfigureCorsOptions(IOptions<CorsSettings> settings) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        var cors = settings.Value;
        var origins = cors.AllowedOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .ToArray();

        options.AddDefaultPolicy(policy =>
        {
            if (origins.Contains("*"))
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(origins);

                if (cors.AllowCredentials)
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
