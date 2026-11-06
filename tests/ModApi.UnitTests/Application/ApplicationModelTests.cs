using Microsoft.Extensions.DependencyInjection;
using ModApi.Application;
using ModApi.Application.Abstractions;
using ModApi.Application.Entities;
using ModApi.Application.Exceptions;
using ModApi.Application.Mapping;
using ModApi.Application.Querying;
using ModApi.Application.Services;
using ModApi.Domain.Entities;
using ModApi.UnitTests.TestDoubles;

namespace ModApi.UnitTests.Application;

public class ApplicationModelTests
{
    [Theory]
    [InlineData(1, 10, 0, 0, false, false)]
    [InlineData(1, 10, 10, 1, false, false)]
    [InlineData(1, 10, 11, 2, false, true)]
    [InlineData(2, 10, 11, 2, true, false)]
    [InlineData(3, 10, 50, 5, true, true)]
    [InlineData(1, 0, 5, 0, false, false)]
    public void PagedResult_computes_navigation(int page, int pageSize, int total, int pages, bool hasPrevious, bool hasNext)
    {
        var result = new PagedResult<int>([], page, pageSize, total);

        Assert.Equal(pages, result.TotalPages);
        Assert.Equal(hasPrevious, result.HasPreviousPage);
        Assert.Equal(hasNext, result.HasNextPage);
    }

    [Fact]
    public void QueryParameters_have_sensible_defaults()
    {
        var parameters = new QueryParameters();

        Assert.Equal(1, parameters.Page);
        Assert.Equal(QueryParameters.DefaultPageSize, parameters.PageSize);
        Assert.Null(parameters.Sort);
        Assert.Null(parameters.Search);
        Assert.Empty(parameters.Filters);
    }

    [Fact]
    public void RequestValidationException_exposes_case_insensitive_errors()
    {
        var exception = new RequestValidationException("Name", "required", "too long");

        Assert.Equal(["required", "too long"], exception.Errors["name"]);
        Assert.Throws<ArgumentNullException>(() => new RequestValidationException((IReadOnlyDictionary<string, string[]>)null!));
    }

    [Fact]
    public void ConflictException_keeps_the_inner_exception()
    {
        var inner = new InvalidOperationException();

        var exception = new ConflictException("conflict", inner);

        Assert.Same(inner, exception.InnerException);
        Assert.Equal("conflict", exception.Message);
    }

    [Fact]
    public void AddApplication_registers_application_services()
    {
        var services = new ServiceCollection().AddApplication(typeof(Product).Assembly);

        using var provider = services.BuildServiceProvider();
        Assert.Contains(provider.GetRequiredService<IEntityRegistry>().Entities, e => e.EntityType == typeof(Product));
        Assert.IsType<ReflectionEntityMapper>(provider.GetRequiredService<IEntityMapper>());
        Assert.IsType<EntityMetadataProvider>(provider.GetRequiredService<IEntityMetadataProvider>());
        Assert.Contains(services, d => d.ServiceType == typeof(ICrudService<,>)
                                       && d.ImplementationType == typeof(CrudService<,>)
                                       && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_does_not_override_existing_registrations()
    {
        var customMapper = Substitute.For<IEntityMapper>();
        var services = new ServiceCollection().AddSingleton(customMapper);

        services.AddApplication(new EntityRegistry([typeof(Widget)]));

        using var provider = services.BuildServiceProvider();
        Assert.Same(customMapper, provider.GetRequiredService<IEntityMapper>());
    }

    [Fact]
    public void AddApplication_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddApplication(new EntityRegistry([typeof(Widget)])));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddApplication((IEntityRegistry)null!));
    }
}
