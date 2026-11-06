using System.Text.Json;
using ModApi.Application.Abstractions;
using ModApi.Application.Exceptions;
using ModApi.Application.Querying;
using ModApi.Application.Services;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class CrudServiceTests
{
    private readonly IRepository<Widget, int> _repository = Substitute.For<IRepository<Widget, int>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEntityMapper _mapper = Substitute.For<IEntityMapper>();
    private readonly CrudService<Widget, int> _service;

    private readonly List<Widget> _store =
    [
        new() { Id = 1, Name = "Alpha", Quantity = 1 },
        new() { Id = 2, Name = "Bravo", Quantity = 2 },
        new() { Id = 3, Name = "Charlie", Quantity = 3 },
    ];

    public CrudServiceTests()
    {
        _service = new CrudService<Widget, int>(_repository, _unitOfWork, _mapper);

        // run the query shapes against a list instead of a database
        _repository.CountAsync(Arg.Any<Func<IQueryable<Widget>, IQueryable<Widget>>?>(), Arg.Any<CancellationToken>())
            .Returns(call => Shape(call.Arg<Func<IQueryable<Widget>, IQueryable<Widget>>?>()).Count());
        _repository.ListAsync(Arg.Any<Func<IQueryable<Widget>, IQueryable<Widget>>?>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyList<Widget>)Shape(call.Arg<Func<IQueryable<Widget>, IQueryable<Widget>>?>()).ToList());
        _repository.FindAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => _store.SingleOrDefault(w => w.Id == call.Arg<int>()));
    }

    private IQueryable<Widget> Shape(Func<IQueryable<Widget>, IQueryable<Widget>>? query) =>
        query is null ? _store.AsQueryable() : query(_store.AsQueryable());

    [Fact]
    public async Task GetPaged_returns_the_requested_page_and_totals()
    {
        var result = await _service.GetPagedAsync(new QueryParameters { Page = 2, PageSize = 2 });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal([3], result.Items.Select(w => w.Id).ToArray());
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetPaged_applies_filters_search_and_sort()
    {
        var result = await _service.GetPagedAsync(new QueryParameters
        {
            Filters = [new FilterCriterion("quantity", FilterOperator.GreaterThanOrEqual, "2")],
            Sort = "-name",
        });

        Assert.Equal([3, 2], result.Items.Select(w => w.Id).ToArray());
        Assert.Equal(2, result.TotalCount);

        var searched = await _service.GetPagedAsync(new QueryParameters { Search = "brav" });
        Assert.Equal(2, Assert.Single(searched.Items).Id);
    }

    [Fact]
    public async Task GetPaged_skips_the_list_query_when_nothing_matches()
    {
        var result = await _service.GetPagedAsync(new QueryParameters
        {
            Filters = [new FilterCriterion("name", FilterOperator.Equal, "nobody")],
        });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        _ = _repository.DidNotReceiveWithAnyArgs().ListAsync(default, default);
    }

    [Theory]
    [InlineData(0, 10, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, QueryParameters.MaxPageSize + 1, "pageSize")]
    public async Task GetPaged_rejects_invalid_paging(int page, int pageSize, string errorKey)
    {
        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => _service.GetPagedAsync(new QueryParameters { Page = page, PageSize = pageSize }));

        Assert.True(exception.Errors.ContainsKey(errorKey));
    }

    [Fact]
    public async Task GetPaged_validates_before_touching_the_repository()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => _service.GetPagedAsync(new QueryParameters { Sort = "bogus" }));
        await Assert.ThrowsAsync<RequestValidationException>(() => _service.GetPagedAsync(new QueryParameters
        {
            Filters = [new FilterCriterion("bogus", FilterOperator.Equal, "1")],
        }));

        _ = _repository.DidNotReceiveWithAnyArgs().CountAsync(default, default);
    }

    [Fact]
    public async Task GetById_returns_the_entity()
    {
        var widget = await _service.GetByIdAsync(2);

        Assert.Equal("Bravo", widget.Name);
    }

    [Fact]
    public async Task GetById_throws_not_found()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(404));

        Assert.Equal("Widget", exception.EntityName);
        Assert.Equal(404, exception.Key);
        Assert.Contains("404", exception.Message);
    }

    [Fact]
    public async Task Create_resets_generated_keys_adds_and_saves()
    {
        var widget = new Widget { Id = 123, Name = "New" };

        var created = await _service.CreateAsync(widget);

        Assert.Same(widget, created);
        Assert.Equal(0, created.Id);
        Received.InOrder(() =>
        {
            _repository.AddAsync(widget, Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Create_keeps_client_supplied_string_keys()
    {
        var repository = Substitute.For<IRepository<Tag, string>>();
        var service = new CrudService<Tag, string>(repository, _unitOfWork, _mapper);

        var created = await service.CreateAsync(new Tag { Id = "dotnet", Title = ".NET" });

        Assert.Equal("dotnet", created.Id);
    }

    [Fact]
    public async Task Create_resets_guid_keys_so_ef_core_generates_them()
    {
        var repository = Substitute.For<IRepository<Gadget, Guid>>();
        var service = new CrudService<Gadget, Guid>(repository, _unitOfWork, _mapper);

        var created = await service.CreateAsync(new Gadget { Id = Guid.NewGuid(), Label = "x" });

        Assert.Equal(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task Update_copies_values_onto_the_tracked_entity_and_saves()
    {
        var incoming = new Widget { Name = "Renamed" };

        var updated = await _service.UpdateAsync(1, incoming);

        Assert.Same(_store[0], updated);
        _mapper.Received(1).CopyValues(incoming, _store[0]);
        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_accepts_a_matching_body_id()
    {
        var updated = await _service.UpdateAsync(1, new Widget { Id = 1, Name = "Same" });

        Assert.Equal(1, updated.Id);
    }

    [Fact]
    public async Task Update_rejects_a_mismatched_body_id()
    {
        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => _service.UpdateAsync(1, new Widget { Id = 2 }));

        Assert.True(exception.Errors.ContainsKey("id"));
        _ = _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Update_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(404, new Widget()));
        _ = _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Patch_applies_the_patch_and_saves()
    {
        var patch = new Dictionary<string, JsonElement> { ["name"] = JsonDocument.Parse("\"Patched\"").RootElement };

        var patched = await _service.PatchAsync(3, patch);

        Assert.Same(_store[2], patched);
        _mapper.Received(1).ApplyPatch(_store[2], patch);
        _ = _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_does_not_save_when_the_patch_is_invalid()
    {
        _mapper.When(m => m.ApplyPatch(Arg.Any<Widget>(), Arg.Any<IReadOnlyDictionary<string, JsonElement>>()))
            .Do(_ => throw new RequestValidationException("name", "bad"));

        await Assert.ThrowsAsync<RequestValidationException>(() => _service.PatchAsync(1, new Dictionary<string, JsonElement>()));
        _ = _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Patch_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.PatchAsync(404, new Dictionary<string, JsonElement>()));
    }

    [Fact]
    public async Task Delete_removes_and_saves()
    {
        await _service.DeleteAsync(2);

        Received.InOrder(() =>
        {
            _repository.Remove(_store[1]);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Delete_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(404));
        _repository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Cancellation_tokens_are_forwarded()
    {
        using var cts = new CancellationTokenSource();

        await _service.GetByIdAsync(1, cts.Token);
        await _service.DeleteAsync(1, cts.Token);

        _ = _repository.Received().FindAsync(1, cts.Token);
        _ = _unitOfWork.Received().SaveChangesAsync(cts.Token);
    }
}
