using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace ModApi.Api.Configuration;

/// <summary>Authentication / authorization registration.</summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication with a "require authenticated user" fallback policy when enabled;
    /// otherwise registers permissive defaults so the middleware pipeline is identical in both modes.
    /// </summary>
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, AuthenticationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
        {
            services.AddAuthentication();
            services.AddAuthorization();
            return services;
        }

        if (string.IsNullOrWhiteSpace(settings.Authority))
        {
            throw new InvalidOperationException("Authentication is enabled but 'Authentication:Authority' is not configured.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                options.Audience = settings.Audience;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                options.MapInboundClaims = false;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
