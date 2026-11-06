using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModApi.Application.Abstractions;
using ModApi.Infrastructure.Database;
using ModApi.Infrastructure.Persistence;
using ModApi.Infrastructure.Persistence.Interceptors;

namespace ModApi.Infrastructure;

/// <summary>Registers infrastructure services (EF Core, providers, repositories, health checks).</summary>
public static class DependencyInjection
{
    /// <summary>Health-check tag used for readiness probes.</summary>
    public const string ReadinessTag = "ready";

    /// <summary>
    /// Registers EF Core with the provider selected from configuration, the generic repository and unit of work.
    /// Requires <c>AddApplication</c> (for <c>IEntityRegistry</c>).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AuditingInterceptor>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProviderConfigurator, SqlServerProviderConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDatabaseProviderConfigurator, PostgreSqlProviderConfigurator>());
        services.TryAddSingleton<IDatabaseProviderResolver, DatabaseProviderResolver>();

        services.AddDbContext<AppDbContext>(ConfigureDbContext);

        services.TryAddScoped(typeof(IReadRepository<,>), typeof(Repository<,>));
        services.TryAddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
        services.TryAddScoped<IDatabaseInitializer, DatabaseInitializer>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: [ReadinessTag]);

        return services;
    }

    /// <summary>Runs the configured database initialization strategy in its own DI scope.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync(cancellationToken);
    }

    private static void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder builder)
    {
        var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{options.ConnectionStringName}' is not configured. " +
                $"Set it in appsettings, user secrets or the 'ConnectionStrings__{options.ConnectionStringName}' environment variable.");
        }

        serviceProvider.GetRequiredService<IDatabaseProviderResolver>()
            .Resolve(options.Provider, connectionString)
            .Configure(builder, connectionString, options);

        builder.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());

        if (options.EnableSensitiveDataLogging)
        {
            builder.EnableSensitiveDataLogging();
        }

        if (options.EnableDetailedErrors)
        {
            builder.EnableDetailedErrors();
        }
    }
}
