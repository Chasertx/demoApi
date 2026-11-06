using Microsoft.EntityFrameworkCore;
using ModApi.Application.Entities;
using ModApi.Application.Exceptions;
using ModApi.Domain.Entities;
using ModApi.Infrastructure.Persistence;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new(new EntityRegistry([typeof(Product).Assembly]));

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SaveChanges_persists_and_returns_affected_rows()
    {
        await using var context = _database.CreateContext();
        context.Add(new Category { Name = "Tools" });

        var affected = await new UnitOfWork(context).SaveChangesAsync();

        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task Unique_constraint_violations_become_conflicts()
    {
        await using var context = _database.CreateContext();
        context.AddRange(
            new Product { Name = "A", Sku = "DUP" },
            new Product { Name = "B", Sku = "DUP" });

        var exception = await Assert.ThrowsAsync<ConflictException>(() => new UnitOfWork(context).SaveChangesAsync());

        Assert.IsAssignableFrom<DbUpdateException>(exception.InnerException);
    }

    [Fact]
    public async Task Concurrency_failures_become_conflicts()
    {
        await using var context = _database.CreateContext();
        var ghost = new Category { Id = 12345, Name = "Ghost" };
        context.Attach(ghost);
        context.Remove(ghost);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => new UnitOfWork(context).SaveChangesAsync());

        Assert.IsType<DbUpdateConcurrencyException>(exception.InnerException);
    }
}
