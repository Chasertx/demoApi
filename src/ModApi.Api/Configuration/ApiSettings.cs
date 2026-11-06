using System.ComponentModel.DataAnnotations;

namespace ModApi.Api.Configuration;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    // "*" allows any origin (credentials are never allowed in that case)
    public string[] AllowedOrigins { get; set; } = [];

    public bool AllowCredentials { get; set; }
}

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    [Range(1, 1_000_000)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; set; }

    public string? Authority { get; set; }

    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;
}

public sealed class SwaggerSettings
{
    public const string SectionName = "Swagger";

    public bool Enabled { get; set; }
}

public sealed class HostingSettings
{
    public const string SectionName = "Hosting";

    // Turn off when TLS is terminated by a proxy in front of the app.
    public bool UseHttpsRedirection { get; set; } = true;
}
