using ModApi.Api;
using ModApi.Application;
using ModApi.Application.Entities;
using ModApi.Domain.Entities;
using ModApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Composition root. Every assembly listed here is scanned for IEntity<TKey> implementations;
// each one becomes part of the EF Core model and gets a full set of CRUD endpoints.
var entityRegistry = new EntityRegistry([typeof(Product).Assembly]);

builder.Services
    .AddApplication(entityRegistry)
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration, entityRegistry);

var app = builder.Build();

app.UsePresentation();
await app.Services.InitializeDatabaseAsync();
await app.RunAsync();

/// <summary>Entry point; declared partial and public so integration tests can reference it.</summary>
public partial class Program
{
}
