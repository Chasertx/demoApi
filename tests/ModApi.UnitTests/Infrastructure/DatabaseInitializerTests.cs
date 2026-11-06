using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModApi.Application.Entities;
using ModApi.Infrastructure.Database;
using ModApi.Infrastructure.Persistence;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly AppDbContext _context;

    public DatabaseInitializerTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _context = new AppDbContext(options, new EntityRegistry([typeof(Widget), typeof(Gadget)]));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private DatabaseInitializer CreateInitializer(DatabaseInitializationStrategy strategy) =>
        new(_context, Options.Create(new DatabaseOptions { InitializationStrategy = strategy }), NullLogger<DatabaseInitializer>.Instance);

    private async Task<bool> WidgetTableExistsAsync()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'Widget'";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    [Fact]
    public async Task None_leaves_the_database_untouched()
    {
        await CreateInitializer(DatabaseInitializationStrategy.None).InitializeAsync();

        Assert.False(await WidgetTableExistsAsync());
    }

    [Fact]
    public async Task EnsureCreated_creates_the_schema_and_is_idempotent()
    {
        var initializer = CreateInitializer(DatabaseInitializationStrategy.EnsureCreated);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.True(await WidgetTableExistsAsync());
    }

    [Fact]
    public async Task Migrate_applies_migrations_without_failing_when_none_exist()
    {
        await CreateInitializer(DatabaseInitializationStrategy.Migrate).InitializeAsync();

        Assert.Empty(await _context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Unknown_strategies_are_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateInitializer((DatabaseInitializationStrategy)99).InitializeAsync());
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        var options = Options.Create(new DatabaseOptions());
        var logger = NullLogger<DatabaseInitializer>.Instance;

        Assert.Throws<ArgumentNullException>(() => new DatabaseInitializer(null!, options, logger));
        Assert.Throws<ArgumentNullException>(() => new DatabaseInitializer(_context, null!, logger));
        Assert.Throws<ArgumentNullException>(() => new DatabaseInitializer(_context, options, null!));
    }
}
