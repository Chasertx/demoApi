using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModApi.Application.Abstractions;
using ModApi.Infrastructure.Database;

namespace ModApi.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    AppDbContext context,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var strategy = options.Value.InitializationStrategy;
        var provider = context.Database.ProviderName;

        switch (strategy)
        {
            case DatabaseInitializationStrategy.None:
                break;

            case DatabaseInitializationStrategy.EnsureCreated:
                var created = await context.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("EnsureCreated on {Provider}, schema created: {Created}", provider, created);
                break;

            case DatabaseInitializationStrategy.Migrate:
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Applied pending migrations on {Provider}", provider);
                break;

            default:
                throw new InvalidOperationException($"Unsupported initialization strategy '{strategy}'.");
        }
    }
}
