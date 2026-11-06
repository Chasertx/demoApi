using System.ComponentModel.DataAnnotations;

namespace ModApi.Api.Configuration;

/// <summary>Cross-origin settings (section <c>Cors</c>). Lets any number of frontends call the API.</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>Allowed origins, e.g. <c>https://app.example.com</c>. Use <c>*</c> to allow any origin (no credentials).</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Allow cookies/authorization headers from the allowed origins (ignored with <c>*</c>).</summary>
    public bool AllowCredentials { get; set; }
}

/// <summary>Per-client fixed-window rate limiting (section <c>RateLimiting</c>).</summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Whether rate limiting is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Requests allowed per client IP per window.</summary>
    [Range(1, 1_000_000)]
    public int PermitLimit { get; set; } = 100;

    /// <summary>Window length in seconds.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>JWT bearer authentication (section <c>Authentication</c>). Works with Entra ID, Auth0, Keycloak, Cognito…</summary>
public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>When enabled every endpoint (except health checks) requires a valid token.</summary>
    public bool Enabled { get; set; }

    /// <summary>The token issuer / OpenID Connect authority URL.</summary>
    public string? Authority { get; set; }

    /// <summary>The expected token audience.</summary>
    public string? Audience { get; set; }

    /// <summary>Require HTTPS for metadata discovery (disable only for local identity servers).</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}

/// <summary>Swagger / OpenAPI UI (section <c>Swagger</c>).</summary>
public sealed class SwaggerSettings
{
    public const string SectionName = "Swagger";

    /// <summary>Serve <c>/swagger</c>. On by default in Development; opt-in elsewhere.</summary>
    public bool Enabled { get; set; }
}

/// <summary>Hosting behaviour (section <c>Hosting</c>).</summary>
public sealed class HostingSettings
{
    public const string SectionName = "Hosting";

    /// <summary>Redirect HTTP to HTTPS. Disable when TLS terminates at a reverse proxy / ingress.</summary>
    public bool UseHttpsRedirection { get; set; } = true;
}
