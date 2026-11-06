using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ModApi.Application;
using ModApi.Application.Abstractions;
using ModApi.Infrastructure;
using ModApi.Infrastructure.Database;
using ModApi.Infrastructure.Persistence;
using ModApi.Infrastructure.Persistence.Interceptors;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public class InfrastructureRegistrationTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication(new ModApi.Application.Entities.EntityRegistry([typeof(Widget)]))
            .AddInfrastructure(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Theory]
    [InlineData("SqlServer", "Server=localhost;Database=x;User Id=sa;Password=p", "Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData("PostgreSql", "Host=localhost;Database=x;Username=u;Password=p", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [InlineData(null, "Host=localhost;Database=x;Username=u;Password=p", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [InlineData(null, "Server=localhost;Database=x;User Id=sa;Password=p", "Microsoft.EntityFrameworkCore.SqlServer")]
    public void The_db_context_uses_the_configured_or_inferred_provider(string? provider, string connectionString, string expectedProvider)
    {
        using var serviceProvider = BuildProvider(new()
        {
            ["Database:Provider"] = provider,
            ["ConnectionStrings:Default"] = connectionString,
        });
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(expectedProvider, context.Database.ProviderName);
    }

    [Fact]
    public void A_named_connection_string_can_be_selected()
    {
        using var serviceProvider = BuildProvider(new()
        {
            ["Database:ConnectionStringName"] = "Reporting",
            ["ConnectionStrings:Reporting"] = "Host=reports;Database=r;Username=u;Password=p",
        });
        using var scope = serviceProvider.CreateScope();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ProviderName);
    }

    [Fact]
    public void A_missing_connection_string_fails_with_a_helpful_message()
    {
        using var serviceProvider = BuildProvider(new());
        using var scope = serviceProvider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AppDbContext>());

        Assert.Contains("ConnectionStrings:Default", exception.Message);
    }

    [Fact]
    public void Repositories_unit_of_work_and_initializer_are_scoped_services()
    {
        using var serviceProvider = BuildProvider(new() { ["ConnectionStrings:Default"] = "Server=x;Database=y;User Id=sa;Password=p" });
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        Assert.IsType<Repository<Widget, int>>(services.GetRequiredService<IRepository<Widget, int>>());
        Assert.IsType<Repository<Widget, int>>(services.GetRequiredService<IReadRepository<Widget, int>>());
        Assert.IsType<UnitOfWork>(services.GetRequiredService<IUnitOfWork>());
        Assert.IsType<DatabaseInitializer>(services.GetRequiredService<IDatabaseInitializer>());
        Assert.Same(services.GetRequiredService<IUnitOfWork>(), services.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void Providers_resolver_clock_and_interceptor_are_registered()
    {
        using var serviceProvider = BuildProvider(new());

        var configurators = serviceProvider.GetServices<IDatabaseProviderConfigurator>().Select(c => c.Name).ToArray();
        Assert.Equal(["SqlServer", "PostgreSql"], configurators);
        Assert.IsType<DatabaseProviderResolver>(serviceProvider.GetRequiredService<IDatabaseProviderResolver>());
        Assert.Same(TimeProvider.System, serviceProvider.GetRequiredService<TimeProvider>());
        Assert.NotNull(serviceProvider.GetRequiredService<AuditingInterceptor>());
    }

    [Fact]
    public void Database_options_are_bound_and_validated()
    {
        using var valid = BuildProvider(new() { ["Database:CommandTimeoutSeconds"] = "90", ["Database:InitializationStrategy"] = "Migrate" });
        var options = valid.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        Assert.Equal(90, options.CommandTimeoutSeconds);
        Assert.Equal(DatabaseInitializationStrategy.Migrate, options.InitializationStrategy);

        using var invalid = BuildProvider(new() { ["Database:CommandTimeoutSeconds"] = "0" });
        Assert.Throws<OptionsValidationException>(() => invalid.GetRequiredService<IOptions<DatabaseOptions>>().Value);
    }

    [Fact]
    public void A_database_health_check_is_registered_for_readiness()
    {
        using var serviceProvider = BuildProvider(new());

        var registration = Assert.Single(serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        Assert.Equal("database", registration.Name);
        Assert.Contains(ModApi.Infrastructure.DependencyInjection.ReadinessTag, registration.Tags);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_runs_the_initializer_in_a_scope()
    {
        var initializer = Substitute.For<IDatabaseInitializer>();
        await using var serviceProvider = new ServiceCollection().AddScoped(_ => initializer).BuildServiceProvider();

        await serviceProvider.InitializeDatabaseAsync();

        _ = initializer.Received(1).InitializeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Sensitive_logging_and_detailed_errors_are_opt_in()
    {
        using var serviceProvider = BuildProvider(new()
        {
            ["ConnectionStrings:Default"] = "Server=x;Database=y;User Id=sa;Password=p",
            ["Database:EnableSensitiveDataLogging"] = "true",
            ["Database:EnableDetailedErrors"] = "true",
        });
        using var scope = serviceProvider.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        var core = options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()!;
        Assert.True(core.IsSensitiveDataLoggingEnabled);
        Assert.True(core.DetailedErrorsEnabled);
    }
}
