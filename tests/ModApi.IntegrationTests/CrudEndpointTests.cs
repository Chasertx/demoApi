using System.Net;
using System.Net.Http.Json;
using ModApi.IntegrationTests.Infrastructure;

namespace ModApi.IntegrationTests;

public sealed class CrudEndpointTests(ModApiFactory factory) : IClassFixture<ModApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 13, 40)];

    private async Task<CategoryDto> CreateCategoryAsync(string? name = null)
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = name ?? Unique("cat"), description = "desc" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsAsync<CategoryDto>();
    }

    [Fact]
    public async Task Full_crud_lifecycle_works_end_to_end()
    {
        // Create
        var create = await _client.PostAsJsonAsync("/api/categories", new { name = "Lifecycle", description = "initial" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.ReadAsAsync<CategoryDto>();
        Assert.True(created.Id > 0);
        Assert.NotEqual(default, created.CreatedAtUtc);
        Assert.Null(created.UpdatedAtUtc);
        Assert.EndsWith($"/api/categories/{created.Id}", create.Headers.Location!.ToString());

        // Read
        var fetched = await _client.GetFromJsonAsync<CategoryDto>($"/api/categories/{created.Id}", ModApiFactory.Json);
        Assert.Equal("Lifecycle", fetched!.Name);

        // Replace
        var put = await _client.PutAsJsonAsync($"/api/categories/{created.Id}", new { name = "Renamed", description = (string?)null });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var replaced = await put.ReadAsAsync<CategoryDto>();
        Assert.Equal("Renamed", replaced.Name);
        Assert.Null(replaced.Description);
        Assert.NotNull(replaced.UpdatedAtUtc);
        Assert.Equal(created.CreatedAtUtc, replaced.CreatedAtUtc, TimeSpan.FromSeconds(1));

        // Patch
        var patch = await _client.PatchAsJsonAsync($"/api/categories/{created.Id}", new { description = "patched" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var patched = await patch.ReadAsAsync<CategoryDto>();
        Assert.Equal("Renamed", patched.Name);
        Assert.Equal("patched", patched.Description);

        // Delete
        var delete = await _client.DeleteAsync($"/api/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/categories/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Guid_keyed_entities_support_crud()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var create = await _client.PostAsJsonAsync("/api/customers", new { firstName = "Ada", lastName = "Lovelace", email });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var customer = await create.ReadAsAsync<CustomerDto>();
        Assert.NotEqual(Guid.Empty, customer.Id);

        var patch = await _client.PatchAsJsonAsync($"/api/customers/{customer.Id}", new { isActive = false });
        Assert.False((await patch.ReadAsAsync<CustomerDto>()).IsActive);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/customers/{customer.Id}")).StatusCode);
    }

    [Fact]
    public async Task Client_supplied_ids_and_audit_fields_are_ignored_on_create()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new
        {
            id = 999_999,
            name = Unique("ids"),
            createdAtUtc = "2000-01-01T00:00:00Z",
        });

        var created = await response.ReadAsAsync<CategoryDto>();
        Assert.NotEqual(999_999, created.Id);
        Assert.True(created.CreatedAtUtc.Year > 2000);
    }

    [Fact]
    public async Task Products_can_reference_categories_and_enums_round_trip_as_strings()
    {
        var category = await CreateCategoryAsync();
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Keyboard",
            sku = Unique("sku"),
            price = 49.95m,
            stockQuantity = 3,
            status = "Draft",
            categoryId = category.Id,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.ReadAsAsync<ProductDto>();
        Assert.Equal("Draft", product.Status);
        Assert.Equal(category.Id, product.CategoryId);
        Assert.Equal(49.95m, product.Price);
    }

    [Fact]
    public async Task Missing_required_fields_return_validation_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { price = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.ReadAsAsync<ProblemResponse>();
        Assert.NotNull(problem.Errors);
        Assert.Contains(problem.Errors.Keys, key => key.Contains("Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Data_annotation_ranges_are_enforced()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = "x", sku = Unique("neg"), price = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_ids_return_404_problem_details()
    {
        var response = await _client.GetAsync("/api/categories/987654321");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadAsAsync<ProblemResponse>();
        Assert.Equal(404, problem.Status);
        Assert.Contains("987654321", problem.Detail);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync("/api/categories/987654321", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsJsonAsync("/api/categories/987654321", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/categories/987654321")).StatusCode);
    }

    [Fact]
    public async Task Malformed_ids_return_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/customers/not-a-guid")).StatusCode);
    }

    [Fact]
    public async Task Mismatched_body_and_route_ids_are_rejected()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.PutAsJsonAsync($"/api/categories/{category.Id}", new { id = category.Id + 1, name = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patching_read_only_or_unknown_fields_is_rejected()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.PatchAsJsonAsync($"/api/categories/{category.Id}", new { id = 1, nope = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsAsync<ProblemResponse>();
        Assert.True(problem.Errors!.ContainsKey("id"));
        Assert.True(problem.Errors.ContainsKey("nope"));
    }

    [Fact]
    public async Task Patching_with_invalid_values_runs_validation()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.PatchAsJsonAsync($"/api/categories/{category.Id}", new { name = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_unique_values_return_409()
    {
        var sku = Unique("dup");
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/products", new { name = "A", sku })).StatusCode);

        var response = await _client.PostAsJsonAsync("/api/products", new { name = "B", sku });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, (await response.ReadAsAsync<ProblemResponse>()).Status);
    }

    [Fact]
    public async Task Unknown_resources_return_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/does-not-exist")).StatusCode);
    }
}
