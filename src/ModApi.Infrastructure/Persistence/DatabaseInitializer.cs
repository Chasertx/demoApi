using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModApi.Application.Abstractions;
using ModApi.Infrastructure.Database;

namespace ModApi.Infrastructure.Persistence;

/// <summary>Runs the configured <see cref="DatabaseInitializationStrategy"/>.</summary>
public sealed partial class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, IOptions<DatabaseOptions> options, ILogger<DatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        switch (_options.InitializationStrategy)
        {
            case DatabaseInitializationStrategy.None:
                LogSkipped();
                break;
            case DatabaseInitializationStrategy.EnsureCreated:
                var created = await _context.Database.EnsureCreatedAsync(cancellationToken);
                LogEnsureCreated(_context.Database.ProviderName, created);
                break;
            case DatabaseInitializationStrategy.Migrate:
                await _context.Database.MigrateAsync(cancellationToken);
                LogMigrated(_context.Database.ProviderName);
                break;
            default:
                throw new InvalidOperationException($"Unsupported initialization strategy '{_options.InitializationStrategy}'.");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Database initialization skipped (strategy: None).")]
    private partial void LogSkipped();

    [LoggerMessage(Level = LogLevel.Information, Message = "EnsureCreated completed for provider {Provider}; schema created: {Created}.")]
    private partial void LogEnsureCreated(string? provider, bool created);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied pending migrations for provider {Provider}.")]
    private partial void LogMigrated(string? provider);
}
