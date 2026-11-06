using Microsoft.EntityFrameworkCore;
using ModApi.Application.Entities;
using ModApi.Infrastructure.Database;
using ModApi.Infrastructure.Persistence;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public class ProviderConfiguratorTests
{
    private const string SqlServerConnection = "Server=localhost,1433;Database=App;User Id=sa;Password=x;TrustServerCertificate=True";
    private const string AzureSqlConnection = "Data Source=tcp:myserver.database.windows.net,1433;Initial Catalog=App;Authentication=Active Directory Default";
    private const string LocalDbConnection = "Server=(localdb)\\MSSQLLocalDB;Database=App;Trusted_Connection=True";
    private const string PostgresConnection = "Host=localhost;Port=5432;Database=app;Username=postgres;Password=x";
    private const string PostgresServerAlias = "Server=db;Port=5432;Database=app;User Id=postgres;Password=x";

    private readonly SqlServerProviderConfigurator _sqlServer = new();
    private readonly PostgreSqlProviderConfigurator _postgres = new();

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("sqlserver")]
    [InlineData(" MsSql ")]
    [InlineData("AzureSql")]
    public void SqlServer_matches_its_aliases(string name)
    {
        Assert.True(_sqlServer.Matches(name));
        Assert.False(_postgres.Matches(name));
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("postgres")]
    [InlineData("Npgsql")]
    [InlineData("PGSQL")]
    public void PostgreSql_matches_its_aliases(string name)
    {
        Assert.True(_postgres.Matches(name));
        Assert.False(_sqlServer.Matches(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Oracle")]
    public void Unknown_names_match_nothing(string name)
    {
        Assert.False(_sqlServer.Matches(name));
        Assert.False(_postgres.Matches(name));
    }

    [Theory]
    [InlineData(SqlServerConnection)]
    [InlineData(AzureSqlConnection)]
    [InlineData(LocalDbConnection)]
    public void SqlServer_recognises_sql_server_connection_strings(string connectionString)
    {
        Assert.True(_sqlServer.CanHandle(connectionString));
        Assert.False(_postgres.CanHandle(connectionString));
    }

    [Theory]
    [InlineData(PostgresConnection)]
    [InlineData(PostgresServerAlias)]
    [InlineData("Host=db;Database=app;Username=u;Password=p;SSL Mode=Require")]
    public void PostgreSql_recognises_postgres_connection_strings(string connectionString)
    {
        Assert.True(_postgres.CanHandle(connectionString));
        Assert.False(_sqlServer.CanHandle(connectionString));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a connection string")]
    [InlineData("Foo=Bar")]
    public void Unrecognised_connection_strings_are_handled_by_nobody(string connectionString)
    {
        Assert.False(_sqlServer.CanHandle(connectionString));
        Assert.False(_postgres.CanHandle(connectionString));
    }

    [Fact]
    public void SqlServer_configures_the_sql_server_provider()
    {
        using var context = CreateContext(_sqlServer, SqlServerConnection, new DatabaseOptions { CommandTimeoutSeconds = 42 });

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(42, context.Database.GetCommandTimeout());
    }

    [Fact]
    public void PostgreSql_configures_the_npgsql_provider()
    {
        using var context = CreateContext(_postgres, PostgresConnection, new DatabaseOptions { CommandTimeoutSeconds = 17 });

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.Equal(17, context.Database.GetCommandTimeout());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Retry_on_failure_is_configurable(bool enabled)
    {
        var options = new DatabaseOptions { EnableRetryOnFailure = enabled, MigrationsAssembly = "Some.Migrations" };

        using var sqlServer = CreateContext(_sqlServer, SqlServerConnection, options);
        using var postgres = CreateContext(_postgres, PostgresConnection, options);

        Assert.Equal(enabled, sqlServer.Database.CreateExecutionStrategy().RetriesOnFailure);
        Assert.Equal(enabled, postgres.Database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void Configure_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => _sqlServer.Configure(null!, SqlServerConnection, new DatabaseOptions()));
        Assert.Throws<ArgumentNullException>(() => _postgres.Configure(new DbContextOptionsBuilder(), PostgresConnection, null!));
    }

    [Fact]
    public void Connection_string_keys_are_parsed_case_insensitively()
    {
        var keys = ConnectionStringKeys.Parse("SERVER=x;Database=y");

        Assert.True(keys.Contains("server"));
        Assert.True(keys.Contains("DATABASE"));
        Assert.Empty(ConnectionStringKeys.Parse(null));
        Assert.Empty(ConnectionStringKeys.Parse("=broken"));
    }

    private static AppDbContext CreateContext(IDatabaseProviderConfigurator configurator, string connectionString, DatabaseOptions options)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        configurator.Configure(builder, connectionString, options);
        return new AppDbContext(builder.Options, new EntityRegistry([typeof(Widget)]));
    }
}
