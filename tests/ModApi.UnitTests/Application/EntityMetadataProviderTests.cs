using ModApi.Application.Entities;
using ModApi.Domain.Entities;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class EntityMetadataProviderTests
{
    private readonly EntityMetadataProvider _provider = new(new EntityRegistry([typeof(Widget), typeof(HiddenThing), typeof(Product)]));

    [Fact]
    public void Only_exposed_entities_are_described()
    {
        Assert.Equal(["Product", "Widget"], _provider.GetAll().Select(m => m.Name).ToArray());
    }

    [Fact]
    public void Results_are_cached()
    {
        Assert.Same(_provider.GetAll(), _provider.GetAll());
    }

    [Fact]
    public void Find_is_case_insensitive_and_returns_null_when_missing()
    {
        Assert.Equal("Widget", _provider.Find("/Widgets")?.Name);
        Assert.Null(_provider.Find("hidden-things"));
        Assert.Null(_provider.Find("nope"));
    }

    [Fact]
    public void Entity_metadata_includes_route_and_key_type()
    {
        var widget = _provider.Find("widgets")!;

        Assert.Equal("widgets", widget.Route);
        Assert.Equal("integer", widget.KeyType);
    }

    [Fact]
    public void Id_comes_first_and_audit_fields_last()
    {
        var names = _provider.Find("widgets")!.Properties.Select(p => p.Name).ToArray();

        Assert.Equal("id", names[0]);
        Assert.Equal(["createdAtUtc", "updatedAtUtc"], names[^2..]);
        Assert.DoesNotContain("gadgets", names);
        Assert.DoesNotContain("computed", names);
    }

    [Fact]
    public void Property_metadata_describes_constraints()
    {
        var properties = _provider.Find("widgets")!.Properties.ToDictionary(p => p.Name);

        var name = properties["name"];
        Assert.Equal("string", name.Type);
        Assert.True(name.IsRequired);
        Assert.False(name.IsNullable);
        Assert.Equal(50, name.MaxLength);
        Assert.False(name.IsReadOnly);

        var notes = properties["notes"];
        Assert.True(notes.IsNullable);
        Assert.False(notes.IsRequired);

        var color = properties["color"];
        Assert.Equal("enum", color.Type);
        Assert.Equal(["Red", "Green", "Blue"], color.EnumValues);

        Assert.True(properties["id"].IsReadOnly);
        Assert.True(properties["createdAtUtc"].IsReadOnly);
        Assert.True(properties["serverManaged"].IsReadOnly);
        Assert.Equal("date-time", properties["shippedAtUtc"].Type);
        Assert.Equal("uuid", properties["externalId"].Type);
        Assert.Equal("number", properties["price"].Type);
        Assert.Equal("boolean", properties["isEnabled"].Type);
    }

    [Theory]
    [InlineData(typeof(int), "integer")]
    [InlineData(typeof(long?), "integer")]
    [InlineData(typeof(byte), "integer")]
    [InlineData(typeof(double), "number")]
    [InlineData(typeof(decimal), "number")]
    [InlineData(typeof(bool), "boolean")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(DateTime), "date-time")]
    [InlineData(typeof(DateTimeOffset), "date-time")]
    [InlineData(typeof(DateOnly), "date")]
    [InlineData(typeof(TimeOnly), "time")]
    [InlineData(typeof(TimeSpan), "time")]
    [InlineData(typeof(Guid), "uuid")]
    [InlineData(typeof(byte[]), "binary")]
    [InlineData(typeof(WidgetColor), "enum")]
    public void DescribeType_maps_clr_types(Type type, string expected)
    {
        Assert.Equal(expected, EntityMetadataProvider.DescribeType(type));
    }
}
