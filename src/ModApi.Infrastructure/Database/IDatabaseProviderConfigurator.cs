using Microsoft.EntityFrameworkCore;

namespace ModApi.Infrastructure.Database;

/// <summary>
/// Strategy for configuring EF Core for one database engine. Add a new engine (MySQL, Oracle, SQLite…)
/// by implementing this interface and registering it – no existing code changes (Open/Closed Principle).
/// </summary>
public interface IDatabaseProviderConfigurator
{
    /// <summary>Canonical provider name, e.g. <c>SqlServer</c>.</summary>
    string Name { get; }

    /// <summary>Whether <paramref name="providerName"/> (from configuration) refers to this provider.</summary>
    bool Matches(string providerName);

    /// <summary>Heuristically decides whether a connection string targets this provider.</summary>
    bool CanHandle(string connectionString);

    /// <summary>Configures <paramref name="builder"/> to use this provider.</summary>
    void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options);
}
