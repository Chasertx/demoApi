using Microsoft.EntityFrameworkCore;

namespace ModApi.Infrastructure.Database;

public interface IDatabaseProviderConfigurator
{
    string Name { get; }

    bool Matches(string providerName);

    bool CanHandle(string connectionString);

    void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options);
}
