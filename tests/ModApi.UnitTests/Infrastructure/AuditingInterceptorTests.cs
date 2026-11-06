using Microsoft.EntityFrameworkCore;
using ModApi.Infrastructure.Persistence.Interceptors;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public sealed class AuditingInterceptorTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Inserts_get_a_created_timestamp_and_no_updated_timestamp()
    {
        await using var context = _database.CreateContext();
        var widget = new Widget { Name = "w", CreatedAtUtc = DateTime.MinValue, UpdatedAtUtc = DateTime.MaxValue };
        context.Add(widget);

        await context.SaveChangesAsync();

        Assert.Equal(SqliteTestDatabase.StartTime.UtcDateTime, widget.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, widget.CreatedAtUtc.Kind);
        Assert.Null(widget.UpdatedAtUtc);
    }

    [Fact]
    public async Task Updates_get_an_updated_timestamp_and_keep_the_created_timestamp()
    {
        int id;
        await using (var context = _database.CreateContext())
        {
            var widget = new Widget { Name = "w" };
            context.Add(widget);
            await context.SaveChangesAsync();
            id = widget.Id;
        }

        _database.Clock.Advance(TimeSpan.FromHours(1));

        await using (var context = _database.CreateContext())
        {
            var widget = await context.Set<Widget>().SingleAsync(w => w.Id == id);
            widget.Name = "changed";
            widget.CreatedAtUtc = DateTime.UnixEpoch; // a client trying to rewrite history
            await context.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        var saved = await verify.Set<Widget>().SingleAsync(w => w.Id == id);
        Assert.Equal(SqliteTestDatabase.StartTime.UtcDateTime, saved.CreatedAtUtc);
        Assert.Equal(SqliteTestDatabase.StartTime.AddHours(1).UtcDateTime, saved.UpdatedAtUtc);
    }

    [Fact]
    public void Synchronous_save_changes_is_audited_too()
    {
        using var context = _database.CreateContext();
        var widget = new Widget { Name = "sync" };
        context.Add(widget);

        context.SaveChanges();

        Assert.Equal(SqliteTestDatabase.StartTime.UtcDateTime, widget.CreatedAtUtc);
    }

    [Fact]
    public async Task Non_auditable_entities_are_ignored()
    {
        await using var context = _database.CreateContext();
        context.Add(new Tag { Id = "t", Title = "Tag" });

        await context.SaveChangesAsync();

        Assert.Single(context.ChangeTracker.Entries<Tag>());
    }

    [Fact]
    public void A_null_context_is_ignored()
    {
        var interceptor = new AuditingInterceptor(TimeProvider.System);

        interceptor.ApplyAuditInformation(null);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new AuditingInterceptor(null!));
    }
}
