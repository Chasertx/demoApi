using Microsoft.EntityFrameworkCore;

namespace ModApi.Infrastructure.Database;

/// <summary>Configures EF Core for PostgreSQL (Npgsql).</summary>
public sealed class PostgreSqlProviderConfigurator : IDatabaseProviderConfigurator
{
    private static readonly string[] Aliases = ["PostgreSql", "Postgres", "Npgsql", "pgsql"];
    private static readonly string[] IndicativeKeys = ["host", "port", "username", "sslmode", "ssl mode", "search path"];

    /// <inheritdoc />
    public string Name => "PostgreSql";

    /// <inheritdoc />
    public bool Matches(string providerName) =>
        !string.IsNullOrWhiteSpace(providerName) && Aliases.Contains(providerName.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool CanHandle(string connectionString)
    {
        var keys = ConnectionStringKeys.Parse(connectionString);
        return IndicativeKeys.Any(keys.Contains);
    }

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.CommandTimeout(options.CommandTimeoutSeconds);

            if (options.EnableRetryOnFailure)
            {
                npgsql.EnableRetryOnFailure(options.MaxRetryCount, TimeSpan.FromSeconds(options.MaxRetryDelaySeconds), null);
            }

            if (!string.IsNullOrWhiteSpace(options.MigrationsAssembly))
            {
                npgsql.MigrationsAssembly(options.MigrationsAssembly);
            }
        });
    }
}
