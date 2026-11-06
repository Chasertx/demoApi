using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModApi.IntegrationTests.Infrastructure;

namespace ModApi.IntegrationTests;

/// <summary>Swagger, health checks, metadata discovery and CORS.</summary>
public sealed class PlatformEndpointTests(ModApiFactory factory) : IClassFixture<ModApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Swagger_document_describes_every_generated_resource()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("/api/products", paths);
        Assert.Contains("/api/products/{id}", paths);
        Assert.Contains("/api/categories/{id}", paths);
        Assert.Contains("/api/customers", paths);
        Assert.Contains("/api/metadata/entities", paths);

        var productOperations = document.RootElement.GetProperty("paths").GetProperty("/api/products/{id}").EnumerateObject().Select(o => o.Name);
        Assert.Equal(["delete", "get", "patch", "put"], productOperations.Order());
    }

    [Fact]
    public async Task Swagger_ui_is_served_and_root_redirects_to_it()
    {
        var ui = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);

        using var noRedirect = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var root = await noRedirect.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, root.StatusCode);
        Assert.Equal("/swagger", root.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_report_healthy(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_includes_the_database_check()
    {
        using var json = JsonDocument.Parse(await _client.GetStringAsync("/health/ready"));

        Assert.Contains(json.RootElement.GetProperty("checks").EnumerateArray(), c => c.GetProperty("name").GetString() == "database");
    }

    [Fact]
    public async Task Metadata_lists_exposed_entities_for_dynamic_frontends()
    {
        var entities = await _client.GetFromJsonAsync<JsonElement>("/api/metadata/entities");

        var routes = entities.EnumerateArray().Select(e => e.GetProperty("route").GetString()!).ToArray();
        Assert.Equal(["categories", "customers", "products"], routes);

        var product = await _client.GetFromJsonAsync<JsonElement>("/api/metadata/entities/products");
        var sku = product.GetProperty("properties").EnumerateArray().Single(p => p.GetProperty("name").GetString() == "sku");
        Assert.True(sku.GetProperty("isRequired").GetBoolean());
        Assert.Equal(64, sku.GetProperty("maxLength").GetInt32());

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/metadata/entities/nope")).StatusCode);
    }

    [Fact]
    public async Task Cors_allows_configured_frontends_only()
    {
        using var allowed = new HttpRequestMessage(HttpMethod.Options, "/api/products");
        allowed.Headers.Add("Origin", "https://frontend.example.com");
        allowed.Headers.Add("Access-Control-Request-Method", "POST");
        var allowedResponse = await _client.SendAsync(allowed);
        Assert.Equal("https://frontend.example.com", allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());

        using var denied = new HttpRequestMessage(HttpMethod.Options, "/api/products");
        denied.Headers.Add("Origin", "https://evil.example.com");
        denied.Headers.Add("Access-Control-Request-Method", "POST");
        var deniedResponse = await _client.SendAsync(denied);
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Problem_details_include_a_trace_id()
    {
        using var json = JsonDocument.Parse(await (await _client.GetAsync("/api/categories/123456789")).Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
    }
}
