using Microsoft.EntityFrameworkCore;
using ModApi.Application.Querying;
using ModApi.Infrastructure.Persistence;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public sealed class RepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public RepositoryTests()
    {
        using var context = _database.CreateContext();
        context.AddRange(
            new Widget { Name = "Alpha", Quantity = 5, Notes = "first", Color = WidgetColor.Red },
            new Widget { Name = "Bravo", Quantity = 10, Color = WidgetColor.Green },
            new Widget { Name = "Charlie", Quantity = 15, Notes = "LAST one", Color = WidgetColor.Blue });
        context.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task List_without_a_query_returns_everything_untracked()
    {
        await using var context = _database.CreateContext();
        var repository = new Repository<Widget, int>(context);

        var widgets = await repository.ListAsync();

        Assert.Equal(3, widgets.Count);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task List_executes_composed_linq_on_the_database()
    {
        await using var context = _database.CreateContext();
        var repository = new Repository<Widget, int>(context);

        var widgets = await repository.ListAsync(q => q.Where(w => w.Quantity > 5).OrderByDescending(w => w.Name));

        Assert.Equal(["Charlie", "Bravo"], widgets.Select(w => w.Name).ToArray());
    }

    [Fact]
    public async Task Dynamic_filter_sort_and_paging_expressions_translate_to_sql()
    {
        await using var context = _database.CreateContext();
        var repository = new Repository<Widget, int>(context);
        var predicate = FilterExpressionBuilder.Build<Widget>(
            [new FilterCriterion("quantity", FilterOperator.GreaterThanOrEqual, "10"), new FilterCriterion("color", FilterOperator.NotEqual, "Red")],
            search: "last");
        var sorts = SortBuilder.Parse<Widget>("-name");

        var widgets = await repository.ListAsync(q => q.Where(predicate!).ApplySort(sorts).ApplyPaging(1, 10));

        Assert.Equal("Charlie", Assert.Single(widgets).Name);
    }

    [Fact]
    public async Task Count_respects_the_query()
    {
        await using var context = _database.CreateContext();
        var repository = new Repository<Widget, int>(context);

        Assert.Equal(3, await repository.CountAsync());
        Assert.Equal(1, await repository.CountAsync(q => q.Where(w => w.Name.StartsWith("A"))));
    }

    [Fact]
    public async Task Find_returns_a_tracked_entity_or_null()
    {
        await using var context = _database.CreateContext();
        var repository = new Repository<Widget, int>(context);

        var found = await repository.FindAsync(1);

        Assert.NotNull(found);
        Assert.Equal(EntityState.Unchanged, context.Entry(found).State);
        Assert.Null(await repository.FindAsync(999));
    }

    [Fact]
    public async Task Add_stages_an_insert()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new Repository<Widget, int>(context);
            await repository.AddAsync(new Widget { Name = "Delta" });
            Assert.Equal(EntityState.Added, context.ChangeTracker.Entries<Widget>().Single().State);
            await context.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(4, await verify.Set<Widget>().CountAsync());
    }

    [Fact]
    public async Task Remove_stages_a_delete()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new Repository<Widget, int>(context);
            repository.Remove((await repository.FindAsync(2))!);
            await context.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        Assert.False(await verify.Set<Widget>().AnyAsync(w => w.Id == 2));
    }

    [Fact]
    public async Task Works_with_guid_and_string_keys()
    {
        await using var context = _database.CreateContext();
        var gadgets = new Repository<Gadget, Guid>(context);
        var tags = new Repository<Tag, string>(context);

        var gadget = new Gadget { Label = "g" };
        await gadgets.AddAsync(gadget);
        await tags.AddAsync(new Tag { Id = "csharp", Title = "C#" });
        await context.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, gadget.Id);
        Assert.NotNull(await gadgets.FindAsync(gadget.Id));
        Assert.Equal("C#", (await tags.FindAsync("csharp"))!.Title);
    }

    [Fact]
    public async Task Arguments_are_validated()
    {
        await using var context = _database.CreateContext();
        var repository = new Repository<Widget, int>(context);

        Assert.Throws<ArgumentNullException>(() => new Repository<Widget, int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
        Assert.Throws<ArgumentNullException>(() => repository.Remove(null!));
    }
}
