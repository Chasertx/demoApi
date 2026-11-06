using Microsoft.EntityFrameworkCore;
using ModApi.Infrastructure.Database;

namespace ModApi.IntegrationTests.Infrastructure;

public sealed class SqliteProviderConfigurator : IDatabaseProviderConfigurator
{
    public string Name => "Sqlite";

    public bool Matches(string providerName) => string.Equals(providerName, Name, StringComparison.OrdinalIgnoreCase);

    // only by name, otherwise "Data Source=" would clash with SQL Server
    public bool CanHandle(string connectionString) => false;

    public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options) =>
        builder.UseSqlite(connectionString, sqlite => sqlite.CommandTimeout(options.CommandTimeoutSeconds));
}
