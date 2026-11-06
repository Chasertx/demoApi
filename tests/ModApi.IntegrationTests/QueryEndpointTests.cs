using System.Net;
using System.Net.Http.Json;
using ModApi.IntegrationTests.Infrastructure;

namespace ModApi.IntegrationTests;

public sealed class QueryEndpointTests : IClassFixture<QueryEndpointTests.SeededFactory>
{
    private readonly HttpClient _client;
    private readonly string _tag;

    public QueryEndpointTests(SeededFactory factory)
    {
        _client = factory.CreateClient();
        _tag = factory.Tag;
    }

    private Task<PagedResponse<ProductDto>> GetAsync(string query) =>
        _client.GetFromJsonAsync<PagedResponse<ProductDto>>($"/api/products?sku:startswith={_tag}&{query}", ModApiFactory.Json)!;

    [Fact]
    public async Task Paging_returns_metadata()
    {
        var page = await GetAsync("page=2&pageSize=2&sort=name");

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(["Charlie", "Delta"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Sorting_supports_multiple_keys_and_directions()
    {
        var page = await GetAsync("sort=-stockQuantity,name");

        Assert.Equal(["Echo", "Alpha", "Bravo", "Delta", "Charlie"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Range_filters_translate_to_sql()
    {
        var page = await GetAsync("stockQuantity:gte=10&stockQuantity:lt=50&sort=name");

        Assert.Equal(["Alpha", "Bravo"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Text_filters_are_case_insensitive()
    {
        var page = await GetAsync("name:contains=HA&sort=name");

        Assert.Equal(["Alpha", "Charlie"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Enum_and_null_filters_work()
    {
        var discontinued = await GetAsync("status=Discontinued");
        Assert.Equal("Charlie", Assert.Single(discontinued.Items).Name);

        var uncategorised = await GetAsync("categoryId=null&sort=name");
        Assert.Equal(["Delta", "Echo"], uncategorised.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Search_matches_any_text_property()
    {
        var page = await GetAsync("search=WIRELESS&sort=name");

        Assert.Equal(["Bravo", "Echo"], page.Items.Select(p => p.Name));
    }

    [Theory]
    [InlineData("sort=bogus")]
    [InlineData("bogus=1")]
    [InlineData("stockQuantity=lots")]
    [InlineData("stockQuantity:between=1")]
    [InlineData("pageSize=1000")]
    [InlineData("page=0")]
    [InlineData("name:gt=a")]
    public async Task Invalid_queries_return_400(string query)
    {
        var response = await _client.GetAsync($"/api/products?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsAsync<ProblemResponse>();
        Assert.NotEmpty(problem.Errors!);
    }

    public sealed class SeededFactory : IAsyncLifetime
    {
        private readonly ModApiFactory _factory = new();

        public string Tag { get; } = $"q{Guid.NewGuid():N}"[..12];

        public HttpClient CreateClient() => _factory.CreateClient();

        public async Task InitializeAsync()
        {
            await _factory.InitializeAsync();
            var client = _factory.CreateClient();

            var category = await (await client.PostAsJsonAsync("/api/categories", new { name = $"{Tag}-cat" }))
                .ReadAsAsync<CategoryDto>();

            object[] products =
            [
                new { name = "Alpha", sku = $"{Tag}-1", stockQuantity = 40, status = "Active", categoryId = category.Id, description = "Wired" },
                new { name = "Bravo", sku = $"{Tag}-2", stockQuantity = 10, status = "Active", categoryId = category.Id, description = "Wireless mouse" },
                new { name = "Charlie", sku = $"{Tag}-3", stockQuantity = 0, status = "Discontinued", categoryId = category.Id },
                new { name = "Delta", sku = $"{Tag}-4", stockQuantity = 5, status = "Draft", categoryId = (int?)null },
                new { name = "Echo", sku = $"{Tag}-5", stockQuantity = 99, status = "Active", categoryId = (int?)null, description = "wireless keyboard" },
            ];

            foreach (var product in products)
            {
                var response = await client.PostAsJsonAsync("/api/products", product);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }
        }

        public async Task DisposeAsync() => await ((IAsyncLifetime)_factory).DisposeAsync();
    }
}
