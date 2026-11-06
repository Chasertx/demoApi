using Microsoft.OpenApi.Models;

namespace ModApi.Api.Configuration;

/// <summary>Swagger / OpenAPI registration.</summary>
public static class SwaggerExtensions
{
    /// <summary>The OpenAPI document name.</summary>
    public const string DocumentName = "v1";

    private static readonly string[] XmlDocumentationFiles = ["ModApi.Api.xml", "ModApi.Application.xml", "ModApi.Domain.xml"];

    /// <summary>Registers the OpenAPI generator, including XML comments and (when enabled) a bearer-token button.</summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, AuthenticationSettings authentication)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(authentication);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "ModApi",
                Version = DocumentName,
                Description = "Generic CRUD API over SQL Server or PostgreSQL. Every entity gets list, get, create, " +
                              "replace, patch and delete endpoints. Use GET /api/metadata/entities to discover resources.",
            });

            foreach (var file in XmlDocumentationFiles)
            {
                var path = Path.Combine(AppContext.BaseDirectory, file);
                if (File.Exists(path))
                {
                    options.IncludeXmlComments(path, includeControllerXmlComments: true);
                }
            }

            options.SupportNonNullableReferenceTypes();

            if (authentication.Enabled)
            {
                var scheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Paste a JWT access token (without the 'Bearer ' prefix).",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                };

                options.AddSecurityDefinition("Bearer", scheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
            }
        });

        return services;
    }
}
