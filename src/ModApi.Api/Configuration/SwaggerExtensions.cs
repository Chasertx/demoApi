using Microsoft.OpenApi.Models;

namespace ModApi.Api.Configuration;

public static class SwaggerExtensions
{
    public const string DocumentName = "v1";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, AuthenticationSettings authentication)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "ModApi",
                Version = DocumentName,
                Description = "CRUD API over SQL Server or PostgreSQL. GET /api/metadata/entities lists the available resources.",
            });

            var xmlDocs = Path.Combine(AppContext.BaseDirectory, "ModApi.Api.xml");
            if (File.Exists(xmlDocs))
            {
                options.IncludeXmlComments(xmlDocs);
            }

            options.SupportNonNullableReferenceTypes();

            if (authentication.Enabled)
            {
                var bearer = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT access token (without the 'Bearer ' prefix).",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                };

                options.AddSecurityDefinition("Bearer", bearer);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = [] });
            }
        });

        return services;
    }
}
