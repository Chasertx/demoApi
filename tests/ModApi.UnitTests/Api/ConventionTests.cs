using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using ModApi.Api.Controllers;
using ModApi.Api.Conventions;
using ModApi.Application.Abstractions;
using ModApi.Application.Entities;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Api;

public class ConventionTests
{
    private readonly EntityRegistry _registry = new([typeof(Widget), typeof(Gadget), typeof(HiddenThing), typeof(RoutedThing)]);

    [Fact]
    public void Feature_provider_adds_a_controller_per_exposed_entity()
    {
        var feature = new ControllerFeature();

        new CrudControllerFeatureProvider(_registry).PopulateFeature([], feature);

        Assert.Equal(
            new[] { typeof(CrudController<Gadget, Guid>), typeof(CrudController<RoutedThing, int>), typeof(CrudController<Widget, int>) }
                .Select(t => t.FullName).Order(),
            feature.Controllers.Select(c => c.FullName).Order());
    }

    [Fact]
    public void Feature_provider_skips_entities_that_have_a_custom_controller()
    {
        var feature = new ControllerFeature();
        feature.Controllers.Add(typeof(CustomWidgetController).GetTypeInfo());

        new CrudControllerFeatureProvider(_registry).PopulateFeature([], feature);

        Assert.DoesNotContain(typeof(CrudController<Widget, int>).GetTypeInfo(), feature.Controllers);
        Assert.Contains(typeof(CustomWidgetController).GetTypeInfo(), feature.Controllers);
        Assert.Contains(typeof(CrudController<Gadget, Guid>).GetTypeInfo(), feature.Controllers);
    }

    [Fact]
    public void Feature_provider_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new CrudControllerFeatureProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new CrudControllerFeatureProvider(_registry).PopulateFeature(Array.Empty<ApplicationPart>(), null!));
    }

    [Theory]
    [InlineData(typeof(CrudController<Widget, int>), typeof(Widget))]
    [InlineData(typeof(CustomWidgetController), typeof(Widget))]
    public void Crud_controller_types_are_recognised(Type controllerType, Type expectedEntity)
    {
        Assert.True(CrudControllerTypes.TryGetEntityType(controllerType, out var entityType));
        Assert.Equal(expectedEntity, entityType);
    }

    [Theory]
    [InlineData(typeof(MetadataController))]
    [InlineData(typeof(object))]
    [InlineData(typeof(CrudController<,>))]
    public void Other_types_are_not_crud_controllers(Type type)
    {
        Assert.False(CrudControllerTypes.TryGetEntityType(type, out var entityType));
        Assert.Null(entityType);
    }

    [Theory]
    [InlineData(typeof(CrudController<Widget, int>), "widgets")]
    [InlineData(typeof(CrudController<RoutedThing, int>), "custom-things")]
    [InlineData(typeof(CustomWidgetController), "widgets")]
    public void Route_convention_names_crud_controllers_after_their_route(Type controllerType, string expected)
    {
        var model = new ControllerModel(controllerType.GetTypeInfo(), []) { ControllerName = "Original" };

        new CrudControllerRouteConvention(_registry).Apply(model);

        Assert.Equal(expected, model.ControllerName);
    }

    [Fact]
    public void Route_convention_leaves_other_controllers_alone()
    {
        var model = new ControllerModel(typeof(MetadataController).GetTypeInfo(), []) { ControllerName = "Metadata" };
        var unregistered = new ControllerModel(typeof(CrudController<Tag, string>).GetTypeInfo(), []) { ControllerName = "Tag" };
        var convention = new CrudControllerRouteConvention(_registry);

        convention.Apply(model);
        convention.Apply(unregistered);

        Assert.Equal("Metadata", model.ControllerName);
        Assert.Equal("Tag", unregistered.ControllerName);
    }

    [Fact]
    public void Route_convention_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new CrudControllerRouteConvention(null!));
        Assert.Throws<ArgumentNullException>(() => new CrudControllerRouteConvention(_registry).Apply(null!));
    }

    public sealed class CustomWidgetController(ICrudService<Widget, int> service) : CrudController<Widget, int>(service);
}
