using Microsoft.EntityFrameworkCore;

namespace ModApi.Infrastructure.Database;

public sealed class PostgreSqlProviderConfigurator : IDatabaseProviderConfigurator
{
    private static readonly string[] Aliases = ["PostgreSql", "Postgres", "Npgsql", "pgsql"];
    private static readonly string[] PostgresKeys = ["host", "port", "username", "sslmode", "ssl mode", "search path"];

    public string Name => "PostgreSql";

    public bool Matches(string providerName) =>
        Aliases.Contains(providerName.Trim(), StringComparer.OrdinalIgnoreCase);

    public bool CanHandle(string connectionString)
    {
        var keys = ConnectionStringKeys.Parse(connectionString);
        return PostgresKeys.Any(keys.Contains);
    }

    public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options)
    {
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
