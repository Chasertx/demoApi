using Microsoft.EntityFrameworkCore;
using ModApi.Application.Entities;
using ModApi.Domain.Entities;
using ModApi.Infrastructure.Persistence;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Infrastructure;

public class AppDbContextTests
{
    [Fact]
    public void Model_contains_every_registered_entity()
    {
        using var database = new SqliteTestDatabase();
        using var context = database.CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(Widget)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Gadget)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Tag)));
        Assert.Null(context.Model.FindEntityType(typeof(Product)));
    }

    [Fact]
    public void Entity_type_configurations_are_applied_for_registered_entities()
    {
        using var database = new SqliteTestDatabase(new EntityRegistry([typeof(Product).Assembly]));
        using var context = database.CreateContext();

        var product = context.Model.FindEntityType(typeof(Product))!;
        var skuIndex = Assert.Single(product.GetIndexes(), i => i.Properties.Single().Name == nameof(Product.Sku));
        Assert.True(skuIndex.IsUnique);
        Assert.Equal(typeof(string), product.FindProperty(nameof(Product.Status))!.GetProviderClrType());

        var customer = context.Model.FindEntityType(typeof(Customer))!;
        Assert.Contains(customer.GetIndexes(), i => i.IsUnique && i.Properties.Single().Name == nameof(Customer.Email));
    }

    [Fact]
    public void Decimal_properties_get_a_default_precision()
    {
        using var database = new SqliteTestDatabase();
        using var context = database.CreateContext();

        var price = context.Model.FindEntityType(typeof(Widget))!.FindProperty(nameof(Widget.Price))!;
        Assert.Equal(18, price.GetPrecision());
        Assert.Equal(4, price.GetScale());
    }

    [Fact]
    public void Contexts_with_different_registries_build_different_models()
    {
        using var first = new SqliteTestDatabase(new EntityRegistry([typeof(Widget), typeof(Gadget)]));
        using var second = new SqliteTestDatabase(new EntityRegistry([typeof(Tag)]));
        using var firstContext = first.CreateContext();
        using var secondContext = second.CreateContext();

        Assert.NotNull(firstContext.Model.FindEntityType(typeof(Widget)));
        Assert.Null(secondContext.Model.FindEntityType(typeof(Widget)));
        Assert.NotNull(secondContext.Model.FindEntityType(typeof(Tag)));
    }

    [Fact]
    public void Model_cache_key_includes_the_registry_signature()
    {
        using var database = new SqliteTestDatabase();
        using var context = database.CreateContext();
        var factory = new EntityRegistryModelCacheKeyFactory();

        var key = factory.Create(context, designTime: false);

        Assert.Equal((typeof(AppDbContext), database.Registry.Signature, false), key);
        Assert.NotEqual(key, factory.Create(context, designTime: true));
    }

    [Fact]
    public void Model_cache_key_for_foreign_contexts_ignores_the_registry()
    {
        using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite("DataSource=:memory:").Options);

        Assert.Equal((typeof(DbContext), false), new EntityRegistryModelCacheKeyFactory().Create(context, false));
    }
}
