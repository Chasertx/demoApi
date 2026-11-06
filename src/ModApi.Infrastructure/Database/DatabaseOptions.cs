using System.ComponentModel.DataAnnotations;

namespace ModApi.Infrastructure.Database;

public enum DatabaseInitializationStrategy
{
    None = 0,
    EnsureCreated = 1,
    Migrate = 2,
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    // SqlServer or PostgreSql. Leave empty to detect it from the connection string.
    public string? Provider { get; set; }

    [Required]
    public string ConnectionStringName { get; set; } = "Default";

    public DatabaseInitializationStrategy InitializationStrategy { get; set; } = DatabaseInitializationStrategy.None;

    public string? MigrationsAssembly { get; set; }

    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool EnableRetryOnFailure { get; set; } = true;

    [Range(0, 20)]
    public int MaxRetryCount { get; set; } = 5;

    [Range(1, 300)]
    public int MaxRetryDelaySeconds { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }
}
