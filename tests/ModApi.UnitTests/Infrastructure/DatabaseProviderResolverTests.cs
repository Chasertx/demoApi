using Microsoft.EntityFrameworkCore;
using ModApi.Infrastructure.Database;

namespace ModApi.UnitTests.Infrastructure;

public class DatabaseProviderResolverTests
{
    private readonly DatabaseProviderResolver _resolver = new([new SqlServerProviderConfigurator(), new PostgreSqlProviderConfigurator()]);

    [Theory]
    [InlineData("SqlServer", "SqlServer")]
    [InlineData("postgres", "PostgreSql")]
    public void Explicit_provider_names_win(string providerName, string expected)
    {
        // connection string deliberately looks like the other provider
        var connectionString = expected == "SqlServer" ? "Host=db;Database=x" : "Server=db;Database=x";

        Assert.Equal(expected, _resolver.Resolve(providerName, connectionString).Name);
    }

    [Theory]
    [InlineData("Server=db;Database=x;User Id=sa;Password=p", "SqlServer")]
    [InlineData("Host=db;Database=x;Username=u;Password=p", "PostgreSql")]
    public void Provider_is_inferred_from_the_connection_string(string connectionString, string expected)
    {
        Assert.Equal(expected, _resolver.Resolve(null, connectionString).Name);
        Assert.Equal(expected, _resolver.Resolve("  ", connectionString).Name);
    }

    [Fact]
    public void Unknown_provider_names_are_rejected_with_the_available_list()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _resolver.Resolve("Oracle", "Server=x"));

        Assert.Contains("Oracle", exception.Message);
        Assert.Contains("SqlServer", exception.Message);
        Assert.Contains("PostgreSql", exception.Message);
    }

    [Fact]
    public void Uninferable_connection_strings_are_rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(null, "Foo=Bar"));

        Assert.Contains("Database:Provider", exception.Message);
    }

    [Fact]
    public void Ambiguous_connection_strings_are_rejected()
    {
        var greedy = Substitute.For<IDatabaseProviderConfigurator>();
        greedy.Name.Returns("Greedy");
        greedy.CanHandle(Arg.Any<string>()).Returns(true);
        var resolver = new DatabaseProviderResolver([new SqlServerProviderConfigurator(), greedy]);

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(null, "Server=x;Database=y"));

        Assert.Contains("Greedy", exception.Message);
    }

    [Fact]
    public void The_last_matching_registration_overrides_earlier_ones()
    {
        var replacement = Substitute.For<IDatabaseProviderConfigurator>();
        replacement.Matches("SqlServer").Returns(true);
        var resolver = new DatabaseProviderResolver([new SqlServerProviderConfigurator(), replacement]);

        Assert.Same(replacement, resolver.Resolve("SqlServer", "Server=x"));
    }

    [Fact]
    public void Custom_providers_can_be_plugged_in()
    {
        var custom = new FakeProvider();
        var resolver = new DatabaseProviderResolver([new SqlServerProviderConfigurator(), custom]);

        Assert.Same(custom, resolver.Resolve("fake", "anything"));
    }

    private sealed class FakeProvider : IDatabaseProviderConfigurator
    {
        public string Name => "Fake";

        public bool Matches(string providerName) => string.Equals(providerName, Name, StringComparison.OrdinalIgnoreCase);

        public bool CanHandle(string connectionString) => false;

        public void Configure(DbContextOptionsBuilder builder, string connectionString, DatabaseOptions options)
        {
        }
    }
}
