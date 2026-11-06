using System.Reflection;
using ModApi.Application.Entities;
using ModApi.Domain.Entities;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class EntityRegistryTests
{
    [Fact]
    public void Scanning_the_domain_assembly_discovers_all_sample_entities()
    {
        var registry = new EntityRegistry([typeof(Product).Assembly]);

        Assert.Equal(
            ["Category", "Customer", "Product"],
            registry.Entities.Select(e => e.Name).ToArray());
        Assert.Equal([typeof(Product).Assembly], registry.Assemblies);
    }

    [Fact]
    public void Descriptor_captures_key_type_route_and_exposure()
    {
        var registry = new EntityRegistry([typeof(Product).Assembly]);

        Assert.True(registry.TryGetByType(typeof(Customer), out var customer));
        Assert.Equal(typeof(Guid), customer.KeyType);
        Assert.Equal("customers", customer.Route);
        Assert.True(customer.IsExposed);

        Assert.True(registry.TryGetByType(typeof(Product), out var product));
        Assert.Equal(typeof(int), product.KeyType);
        Assert.Equal("products", product.Route);
    }

    [Fact]
    public void Crud_resource_attribute_overrides_the_route()
    {
        var registry = new EntityRegistry([typeof(RoutedThing)]);

        Assert.True(registry.TryGetByType(typeof(RoutedThing), out var descriptor));
        Assert.Equal("custom-things", descriptor.Route);
    }

    [Fact]
    public void Crud_resource_attribute_can_hide_an_entity()
    {
        var registry = new EntityRegistry([typeof(HiddenThing)]);

        Assert.False(Assert.Single(registry.Entities).IsExposed);
    }

    [Fact]
    public void Multi_word_names_become_kebab_case_plural_routes()
    {
        var registry = new EntityRegistry([typeof(OrderLine)]);

        Assert.Equal("order-lines", Assert.Single(registry.Entities).Route);
    }

    [Fact]
    public void Entities_implementing_IEntity_directly_are_supported()
    {
        var registry = new EntityRegistry([typeof(Tag), typeof(Measurement)]);

        Assert.True(registry.TryGetByType(typeof(Tag), out var tag));
        Assert.Equal(typeof(string), tag.KeyType);
        Assert.True(registry.TryGetByType(typeof(Measurement), out var measurement));
        Assert.Equal(typeof(long), measurement.KeyType);
    }

    [Fact]
    public void Lookup_by_route_is_case_insensitive_and_ignores_slashes()
    {
        var registry = new EntityRegistry([typeof(Widget)]);

        Assert.True(registry.TryGetByRoute("/WIDGETS/", out var descriptor));
        Assert.Equal(typeof(Widget), descriptor.EntityType);
        Assert.False(registry.TryGetByRoute("unknown", out _));
    }

    [Fact]
    public void Lookup_of_unregistered_type_returns_false()
    {
        var registry = new EntityRegistry([typeof(Widget)]);

        Assert.False(registry.TryGetByType(typeof(Gadget), out _));
    }

    [Fact]
    public void Duplicate_routes_are_rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new EntityRegistry([typeof(OrderLine), typeof(ConflictingOrderLine)]));

        Assert.Contains("order-lines", exception.Message);
    }

    [Fact]
    public void Types_with_two_key_types_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new EntityRegistry([typeof(TwoKeys)]));
    }

    [Theory]
    [InlineData(typeof(AbstractThing))]
    [InlineData(typeof(NotAnEntity))]
    public void Non_entity_types_are_rejected(Type type)
    {
        Assert.Throws<ArgumentException>(() => new EntityRegistry([type]));
    }

    [Fact]
    public void An_empty_assembly_list_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new EntityRegistry(Array.Empty<Assembly>()));
    }

    [Fact]
    public void Duplicated_types_are_registered_once()
    {
        var registry = new EntityRegistry([typeof(Widget), typeof(Widget)]);

        Assert.Single(registry.Entities);
    }

    [Fact]
    public void Signature_is_stable_and_distinguishes_entity_sets()
    {
        var first = new EntityRegistry([typeof(Widget), typeof(Gadget)]);
        var same = new EntityRegistry([typeof(Gadget), typeof(Widget)]);
        var different = new EntityRegistry([typeof(Widget)]);

        Assert.Equal(first.Signature, same.Signature);
        Assert.NotEqual(first.Signature, different.Signature);
    }

    [Theory]
    [InlineData(typeof(Widget), typeof(int))]
    [InlineData(typeof(Gadget), typeof(Guid))]
    [InlineData(typeof(Tag), typeof(string))]
    public void TryGetKeyType_returns_the_key(Type entityType, Type expected)
    {
        Assert.True(EntityRegistry.TryGetKeyType(entityType, out var keyType));
        Assert.Equal(expected, keyType);
    }

    [Fact]
    public void TryGetKeyType_fails_for_non_entities_and_ambiguous_keys()
    {
        Assert.False(EntityRegistry.TryGetKeyType(typeof(NotAnEntity), out _));
        Assert.False(EntityRegistry.TryGetKeyType(typeof(TwoKeys), out _));
    }

    [Theory]
    [InlineData(typeof(Widget), true)]
    [InlineData(typeof(AbstractThing), false)]
    [InlineData(typeof(NotAnEntity), false)]
    [InlineData(typeof(string), false)]
    public void IsEntityType_classifies_types(Type type, bool expected)
    {
        Assert.Equal(expected, EntityRegistry.IsEntityType(type));
    }
}
