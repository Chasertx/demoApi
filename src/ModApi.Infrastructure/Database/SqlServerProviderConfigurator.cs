using Microsoft.EntityFrameworkCore;

namespace ModApi.Infrastructure.Database;

/// <summary>Configures EF Core for Microsoft SQL Server / Azure SQL.</summary>
public sealed class SqlServerProviderConfigurator : IDatabaseProviderConfigurator
{
    private static readonly string[] Aliases = ["SqlServer", "MsSql", "Sql Server", "AzureSql"];
    private static readonly string[] IndicativeKeys = ["server", "data source", "initial catalog", "integrated security", "trusted_connection", "address", "addr"];
    private static readonly string[] ForeignKeys = ["host", "port", "username"];

    /// <inheritdoc />
    public string Name => "SqlServer";

    /// <inheritdoc />
    public bool Matches(string providerName) =>
        !string.IsNullOrWhiteSpace(providerName) && Aliases.Contains(providerName.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool CanHandle(string connectionString)
    {
        var keys = ConnectionStringKeys.Parse(connectionString);
        return IndicativeKeys.Any(keys.Contains) && !ForeignKeys.Any(keys.Contains);
    }

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.UseSqlServer(connectionString, sql =>
        {
            sql.CommandTimeout(options.CommandTimeoutSeconds);

            if (options.EnableRetryOnFailure)
            {
                sql.EnableRetryOnFailure(options.MaxRetryCount, TimeSpan.FromSeconds(options.MaxRetryDelaySeconds), null);
            }

            if (!string.IsNullOrWhiteSpace(options.MigrationsAssembly))
            {
                sql.MigrationsAssembly(options.MigrationsAssembly);
            }
        });
    }
}
