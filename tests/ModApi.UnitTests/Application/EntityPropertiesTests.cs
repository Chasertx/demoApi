using ModApi.Application.Reflection;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class EntityPropertiesTests
{
    private readonly EntityPropertySet _widget = EntityProperties.For(typeof(Widget));

    [Fact]
    public void Results_are_cached_per_type()
    {
        Assert.Same(_widget, EntityProperties.For(typeof(Widget)));
        Assert.Equal(typeof(Widget), _widget.EntityType);
    }

    [Fact]
    public void Scalar_excludes_navigations_and_not_mapped_properties()
    {
        var names = _widget.Scalar.Select(p => p.Name).ToHashSet();

        Assert.Contains("Name", names);
        Assert.Contains("Id", names);
        Assert.Contains("CreatedAtUtc", names);
        Assert.DoesNotContain("Gadgets", names);
        Assert.DoesNotContain("Computed", names);
    }

    [Fact]
    public void Mutable_excludes_keys_audit_fields_and_read_only_properties()
    {
        var names = _widget.Mutable.Select(p => p.Name).ToHashSet();

        Assert.Contains("Name", names);
        Assert.Contains("Price", names);
        Assert.DoesNotContain("Id", names);
        Assert.DoesNotContain("CreatedAtUtc", names);
        Assert.DoesNotContain("UpdatedAtUtc", names);
        Assert.DoesNotContain("ServerManaged", names);
    }

    [Fact]
    public void Text_contains_only_string_properties()
    {
        Assert.All(_widget.Text, p => Assert.Equal(typeof(string), p.PropertyType));
        Assert.Contains(_widget.Text, p => p.Name == "Name");
        Assert.Contains(_widget.Text, p => p.Name == "Notes");
    }

    [Fact]
    public void Lookups_are_case_insensitive()
    {
        Assert.True(_widget.TryGetQueryable("nAmE", out var queryable));
        Assert.Equal("Name", queryable.Name);
        Assert.True(_widget.TryGetMutable("PRICE", out var mutable));
        Assert.Equal("Price", mutable.Name);
        Assert.False(_widget.TryGetMutable("id", out _));
        Assert.False(_widget.TryGetQueryable("gadgets", out _));
    }

    [Fact]
    public void Nullability_is_detected_for_value_and_reference_types()
    {
        bool IsNullable(string name) => _widget.IsNullable(_widget.Scalar.Single(p => p.Name == name));

        Assert.True(IsNullable("Notes"));
        Assert.True(IsNullable("ShippedAtUtc"));
        Assert.True(IsNullable("ExternalId"));
        Assert.False(IsNullable("Name"));
        Assert.False(IsNullable("Quantity"));
    }

    [Fact]
    public void System_properties_are_identified()
    {
        bool IsSystem(string name) => _widget.IsSystemProperty(_widget.Scalar.Single(p => p.Name == name));

        Assert.True(IsSystem("Id"));
        Assert.True(IsSystem("CreatedAtUtc"));
        Assert.False(IsSystem("Name"));
    }

    [Fact]
    public void Audit_named_properties_are_not_system_properties_on_non_auditable_entities()
    {
        var tag = EntityProperties.For(typeof(Tag));

        Assert.True(tag.TryGetMutable("Title", out _));
        Assert.False(tag.TryGetMutable("Id", out _));
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(TimeOnly), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(WidgetColor), true)]
    [InlineData(typeof(byte[]), true)]
    [InlineData(typeof(Gadget), false)]
    [InlineData(typeof(List<int>), false)]
    public void IsScalarType_classifies_types(Type type, bool expected)
    {
        Assert.Equal(expected, EntityProperties.IsScalarType(type));
    }
}
