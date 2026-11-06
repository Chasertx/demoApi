using ModApi.Api;
using ModApi.Application;
using ModApi.Application.Entities;
using ModApi.Domain.Entities;
using ModApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Every IEntity<TKey> in these assemblies gets a table and a set of CRUD endpoints.
var entityRegistry = new EntityRegistry([typeof(Product).Assembly]);

builder.Services
    .AddApplication(entityRegistry)
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration, entityRegistry);

var app = builder.Build();

app.UsePresentation();
await app.Services.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program
{
}
