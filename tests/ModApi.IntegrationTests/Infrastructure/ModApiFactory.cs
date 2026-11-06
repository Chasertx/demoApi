using System.Data.Common;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using ModApi.Infrastructure.Database;
using ModApi.Infrastructure.Persistence;

namespace ModApi.IntegrationTests.Infrastructure;

public sealed class ModApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = CreateJsonOptions();

    private readonly string _provider;
    private readonly string _connectionString;

    public ModApiFactory()
    {
        _provider = Environment.GetEnvironmentVariable("MODAPI_TEST_PROVIDER") is { Length: > 0 } provider ? provider : "Sqlite";
        var databaseName = $"modapi_test_{Guid.NewGuid():N}";

        if (string.Equals(_provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(Path.GetTempPath(), $"{databaseName}.db"),
                Pooling = false,
            }.ToString();
        }
        else
        {
            var template = Environment.GetEnvironmentVariable("MODAPI_TEST_CONNECTION")
                ?? throw new InvalidOperationException("MODAPI_TEST_CONNECTION must be set when MODAPI_TEST_PROVIDER is not Sqlite.");
            var builder = new DbConnectionStringBuilder { ConnectionString = template };
            builder.Remove("Initial Catalog");
            builder["Database"] = databaseName;
            _connectionString = builder.ConnectionString;
        }
    }

    public string ProviderName => _provider;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting so the values are already there while Program.cs is registering services
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Database:Provider", _provider);
        builder.UseSetting("Database:InitializationStrategy", nameof(DatabaseInitializationStrategy.EnsureCreated));
        builder.UseSetting("Database:EnableRetryOnFailure", "false");
        builder.UseSetting("Swagger:Enabled", "true");
        builder.UseSetting("RateLimiting:Enabled", "false");
        builder.UseSetting("Hosting:UseHttpsRedirection", "false");
        builder.UseSetting("Cors:AllowedOrigins:0", "https://frontend.example.com");

        builder.ConfigureServices(services =>
            services.AddSingleton<IDatabaseProviderConfigurator, SqliteProviderConfigurator>());
    }

    public Task InitializeAsync()
    {
        // start the host now so the database is created before the first test
        _ = Server;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }
        finally
        {
            await base.DisposeAsync();
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public static class HttpContentExtensions
{
    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(ModApiFactory.Json);
        return value ?? throw new InvalidOperationException("The response body was empty.");
    }
}
