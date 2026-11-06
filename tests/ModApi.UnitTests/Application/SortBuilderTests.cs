using ModApi.Application.Exceptions;
using ModApi.Application.Querying;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class SortBuilderTests
{
    private static readonly Widget[] Widgets =
    [
        new() { Id = 3, Name = "Charlie", Quantity = 1 },
        new() { Id = 1, Name = "Alpha", Quantity = 2 },
        new() { Id = 4, Name = "Bravo", Quantity = 1 },
        new() { Id = 2, Name = "Alpha", Quantity = 3 },
    ];

    private static int[] Sort(string? sort) =>
        Widgets.AsQueryable().ApplySort(SortBuilder.Parse<Widget>(sort)).Select(w => w.Id).ToArray();

    [Fact]
    public void Parse_returns_empty_for_no_sort()
    {
        Assert.Empty(SortBuilder.Parse<Widget>(null));
        Assert.Empty(SortBuilder.Parse<Widget>(" "));
    }

    [Fact]
    public void Parse_reads_direction_prefixes()
    {
        var sorts = SortBuilder.Parse<Widget>("name, -quantity, +id");

        Assert.Collection(
            sorts,
            s => { Assert.Equal("Name", s.Property.Name); Assert.False(s.Descending); },
            s => { Assert.Equal("Quantity", s.Property.Name); Assert.True(s.Descending); },
            s => { Assert.Equal("Id", s.Property.Name); Assert.False(s.Descending); });
    }

    [Fact]
    public void Parse_ignores_duplicate_properties()
    {
        Assert.Single(SortBuilder.Parse<Widget>("name,-name"));
    }

    [Fact]
    public void Parse_rejects_unknown_properties()
    {
        var exception = Assert.Throws<RequestValidationException>(() => SortBuilder.Parse<Widget>("name,bogus,gadgets"));

        Assert.Equal(2, exception.Errors["sort"].Length);
    }

    [Fact]
    public void Default_order_is_by_id()
    {
        Assert.Equal([1, 2, 3, 4], Sort(null));
    }

    [Fact]
    public void Ascending_sort_uses_id_as_tie_breaker()
    {
        Assert.Equal([1, 2, 4, 3], Sort("name"));
    }

    [Fact]
    public void Descending_sort_is_supported()
    {
        Assert.Equal([3, 4, 1, 2], Sort("-name"));
    }

    [Fact]
    public void Multiple_sort_keys_are_applied_in_order()
    {
        Assert.Equal([3, 4, 1, 2], Sort("quantity,-name"));
    }

    [Fact]
    public void Explicit_id_sort_is_not_duplicated()
    {
        Assert.Equal([4, 3, 2, 1], Sort("-id"));
    }

    [Fact]
    public void Entities_without_an_id_property_can_still_be_sorted()
    {
        var items = new[] { "b", "a" }.AsQueryable();

        Assert.Equal(["b", "a"], items.ApplySort([]).ToArray());
    }

    [Fact]
    public void Paging_skips_and_takes()
    {
        var page = Enumerable.Range(1, 10).AsQueryable().ApplyPaging(page: 2, pageSize: 3).ToArray();

        Assert.Equal([4, 5, 6], page);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void Paging_rejects_invalid_arguments(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, 5).AsQueryable().ApplyPaging(page, pageSize));
    }
}
