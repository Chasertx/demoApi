using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModApi.Api.Controllers;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;
using ModApi.Application.Querying;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Api;

public class CrudControllerTests
{
    private readonly ICrudService<Widget, int> _service = Substitute.For<ICrudService<Widget, int>>();
    private readonly CrudController<Widget, int> _controller;

    public CrudControllerTests()
    {
        _controller = new CrudController<Widget, int>(_service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        _controller.HttpContext.Request.Path = "/api/widgets";
    }

    private void SetQuery(Dictionary<string, StringValues> query) =>
        _controller.HttpContext.Request.Query = new QueryCollection(query);

    [Fact]
    public async Task GetAll_maps_paging_sort_search_and_filters()
    {
        SetQuery(new()
        {
            ["page"] = "2",
            ["PageSize"] = "5",
            ["sort"] = "-name",
            ["search"] = "lap",
            ["quantity:gte"] = "3",
            ["color"] = "Red",
        });
        var expected = new PagedResult<Widget>([new Widget { Id = 1 }], 2, 5, 6);
        QueryParameters? captured = null;
        _service.GetPagedAsync(Arg.Do<QueryParameters>(p => captured = p), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.GetAll(page: 2, pageSize: 5, sort: "-name", search: "lap");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.NotNull(captured);
        Assert.Equal(2, captured.Page);
        Assert.Equal(5, captured.PageSize);
        Assert.Equal("-name", captured.Sort);
        Assert.Equal("lap", captured.Search);
        Assert.Equal(2, captured.Filters.Count);
        Assert.Contains(new FilterCriterion("quantity", FilterOperator.GreaterThanOrEqual, "3"), captured.Filters);
        Assert.Contains(new FilterCriterion("color", FilterOperator.Equal, "Red"), captured.Filters);
    }

    [Fact]
    public async Task GetAll_without_query_uses_defaults()
    {
        QueryParameters? captured = null;
        _service.GetPagedAsync(Arg.Do<QueryParameters>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Widget>([], 1, QueryParameters.DefaultPageSize, 0));

        await _controller.GetAll();

        Assert.NotNull(captured);
        Assert.Equal(1, captured.Page);
        Assert.Equal(QueryParameters.DefaultPageSize, captured.PageSize);
        Assert.Empty(captured.Filters);
    }

    [Fact]
    public async Task GetAll_propagates_invalid_filter_operators_as_validation_errors()
    {
        SetQuery(new() { ["quantity:between"] = "1" });

        await Assert.ThrowsAsync<RequestValidationException>(() => _controller.GetAll());
    }

    [Fact]
    public async Task GetById_returns_ok_with_the_entity()
    {
        var widget = new Widget { Id = 3 };
        _service.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(widget);

        var result = await _controller.GetById(3);

        Assert.Same(widget, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GetById_propagates_not_found()
    {
        _service.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns<Widget>(_ => throw new NotFoundException("Widget", 9));

        await Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(9));
    }

    [Fact]
    public async Task Create_returns_created_with_location()
    {
        var input = new Widget { Name = "New" };
        _service.CreateAsync(input, Arg.Any<CancellationToken>()).Returns(new Widget { Id = 42, Name = "New" });

        var result = await _controller.Create(input);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("/api/widgets/42", created.Location);
        Assert.Equal(42, Assert.IsType<Widget>(created.Value).Id);
    }

    [Fact]
    public async Task Create_escapes_string_keys_in_the_location()
    {
        var service = Substitute.For<ICrudService<Tag, string>>();
        service.CreateAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()).Returns(new Tag { Id = "c# tips" });
        var controller = new CrudController<Tag, string>(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.HttpContext.Request.Path = "/api/tags/";

        var result = await controller.Create(new Tag());

        Assert.Equal("/api/tags/c%23%20tips", Assert.IsType<CreatedResult>(result.Result).Location);
    }

    [Fact]
    public async Task Update_returns_ok_with_the_updated_entity()
    {
        var input = new Widget { Name = "Changed" };
        var updated = new Widget { Id = 5, Name = "Changed" };
        _service.UpdateAsync(5, input, Arg.Any<CancellationToken>()).Returns(updated);

        var result = await _controller.Update(5, input);

        Assert.Same(updated, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Patch_returns_ok_with_the_patched_entity()
    {
        var patch = new Dictionary<string, JsonElement> { ["quantity"] = JsonDocument.Parse("7").RootElement };
        var patched = new Widget { Id = 5, Quantity = 7 };
        _service.PatchAsync(5, patch, Arg.Any<CancellationToken>()).Returns(patched);

        var result = await _controller.Patch(5, patch);

        Assert.Same(patched, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Delete_returns_no_content()
    {
        var result = await _controller.Delete(5);

        Assert.IsType<NoContentResult>(result);
        _ = _service.Received(1).DeleteAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_is_forwarded_to_the_service()
    {
        using var cts = new CancellationTokenSource();

        await _controller.Delete(1, cts.Token);

        _ = _service.Received(1).DeleteAsync(1, cts.Token);
    }
}
