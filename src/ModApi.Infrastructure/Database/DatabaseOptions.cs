using System.ComponentModel.DataAnnotations;

namespace ModApi.Infrastructure.Database;

/// <summary>How the database schema is prepared at startup.</summary>
public enum DatabaseInitializationStrategy
{
    /// <summary>Do nothing (recommended in production; run migrations from the pipeline).</summary>
    None = 0,

    /// <summary>Create the database and schema from the model if missing (no migrations; ideal for dev/test).</summary>
    EnsureCreated = 1,

    /// <summary>Apply pending EF Core migrations from <see cref="DatabaseOptions.MigrationsAssembly"/>.</summary>
    Migrate = 2,
}

/// <summary>Database settings bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Provider name (<c>SqlServer</c> or <c>PostgreSql</c>, case-insensitive). Leave empty to infer it from the connection string.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>Name of the entry under <c>ConnectionStrings</c> to use. Defaults to <c>Default</c>.</summary>
    [Required]
    public string ConnectionStringName { get; set; } = "Default";

    /// <summary>Schema initialization at startup. Defaults to <see cref="DatabaseInitializationStrategy.None"/>.</summary>
    public DatabaseInitializationStrategy InitializationStrategy { get; set; } = DatabaseInitializationStrategy.None;

    /// <summary>Assembly containing provider-specific migrations (optional).</summary>
    public string? MigrationsAssembly { get; set; }

    /// <summary>Command timeout in seconds.</summary>
    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Retry transient failures (recommended for cloud databases).</summary>
    public bool EnableRetryOnFailure { get; set; } = true;

    /// <summary>Maximum retry attempts for transient failures.</summary>
    [Range(0, 20)]
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>Maximum delay between retries in seconds.</summary>
    [Range(1, 300)]
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>Log parameter values (development only – may leak sensitive data).</summary>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>Include detailed EF Core errors (development only).</summary>
    public bool EnableDetailedErrors { get; set; }
}
