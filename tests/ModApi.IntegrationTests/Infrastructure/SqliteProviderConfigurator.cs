using Microsoft.EntityFrameworkCore;
using ModApi.Infrastructure.Database;

namespace ModApi.IntegrationTests.Infrastructure;

/// <summary>
/// A third database provider plugged in purely through DI – demonstrating the Open/Closed design of
/// <see cref="IDatabaseProviderConfigurator"/> and giving tests a fast, dependency-free relational database.
/// </summary>
public sealed class SqliteProviderConfigurator : IDatabaseProviderConfigurator
{
    public string Name => "Sqlite";

    public bool Matches(string providerName) => string.Equals(providerName, Name, StringComparison.OrdinalIgnoreCase);

    // Only used when selected explicitly, so it never competes with SQL Server for "Data Source=" strings.
    public bool CanHandle(string connectionString) => false;

    public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options) =>
        builder.UseSqlite(connectionString, sqlite => sqlite.CommandTimeout(options.CommandTimeoutSeconds));
}
