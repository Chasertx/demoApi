using Microsoft.AspNetCore.Mvc;
using ModApi.Api.Controllers;
using ModApi.Application.Entities;

namespace ModApi.UnitTests.Api;

public class MetadataControllerTests
{
    private readonly IEntityMetadataProvider _provider = Substitute.For<IEntityMetadataProvider>();

    [Fact]
    public void GetEntities_returns_all_metadata()
    {
        IReadOnlyList<EntityMetadata> all = [new EntityMetadata("Widget", "widgets", "integer", [])];
        _provider.GetAll().Returns(all);

        var result = new MetadataController(_provider).GetEntities();

        Assert.Same(all, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public void GetEntity_returns_the_entity_or_not_found()
    {
        var widget = new EntityMetadata("Widget", "widgets", "integer", []);
        _provider.Find("widgets").Returns(widget);
        var controller = new MetadataController(_provider);

        Assert.Same(widget, Assert.IsType<OkObjectResult>(controller.GetEntity("widgets").Result).Value);
        Assert.IsType<NotFoundResult>(controller.GetEntity("missing").Result);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new MetadataController(null!));
    }
}
