using Microsoft.EntityFrameworkCore;

namespace ModApi.Infrastructure.Database;

public sealed class SqlServerProviderConfigurator : IDatabaseProviderConfigurator
{
    private static readonly string[] Aliases = ["SqlServer", "MsSql", "Sql Server", "AzureSql"];
    private static readonly string[] SqlServerKeys = ["server", "data source", "initial catalog", "integrated security", "trusted_connection", "address", "addr"];

    // Npgsql also accepts "Server", so back off if anything Postgres-only shows up.
    private static readonly string[] PostgresKeys = ["host", "port", "username"];

    public string Name => "SqlServer";

    public bool Matches(string providerName) =>
        Aliases.Contains(providerName.Trim(), StringComparer.OrdinalIgnoreCase);

    public bool CanHandle(string connectionString)
    {
        var keys = ConnectionStringKeys.Parse(connectionString);
        return SqlServerKeys.Any(keys.Contains) && !PostgresKeys.Any(keys.Contains);
    }

    public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options)
    {
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
