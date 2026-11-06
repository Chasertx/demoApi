using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace ModApi.Api.Configuration;

public static class SecurityExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, AuthenticationSettings settings)
    {
        if (!settings.Enabled)
        {
            services.AddAuthentication();
            services.AddAuthorization();
            return services;
        }

        if (string.IsNullOrWhiteSpace(settings.Authority))
        {
            throw new InvalidOperationException("Authentication is enabled but 'Authentication:Authority' is not set.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = settings.Authority;
                options.Audience = settings.Audience;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                options.MapInboundClaims = false;
            });

        // Everything requires a signed-in user unless it opts out with [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
