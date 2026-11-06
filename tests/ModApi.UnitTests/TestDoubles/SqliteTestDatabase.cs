using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModApi.Application.Entities;
using ModApi.Infrastructure.Persistence;
using ModApi.Infrastructure.Persistence.Interceptors;

namespace ModApi.UnitTests.TestDoubles;

/// <summary>
/// An isolated in-memory SQLite database. SQLite is a real relational engine, so constraints and
/// LINQ translation behave like production providers (unlike the EF in-memory provider).
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    public static readonly DateTimeOffset StartTime = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;

    public SqliteTestDatabase(IEntityRegistry? registry = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Registry = registry ?? new EntityRegistry([typeof(Widget), typeof(Gadget), typeof(Tag)]);
        Clock = new FakeTimeProvider(StartTime);
        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditingInterceptor(Clock))
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public IEntityRegistry Registry { get; }

    public FakeTimeProvider Clock { get; }

    public DbContextOptions<AppDbContext> Options { get; }

    public AppDbContext CreateContext() => new(Options, Registry);

    public void Dispose() => _connection.Dispose();
}
