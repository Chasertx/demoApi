using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModApi.Application.Abstractions;
using ModApi.Application.Querying;
using ModApi.Domain.Common;

namespace ModApi.Api.Controllers;

/// <summary>
/// Generic REST controller. A closed instance (e.g. <c>CrudController&lt;Product, int&gt;</c>) is generated for every
/// exposed entity by <see cref="Conventions.CrudControllerFeatureProvider"/> and routed at <c>api/{route}</c>
/// by <see cref="Conventions.CrudControllerRouteConvention"/>. Subclass it to customise one entity.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CrudController<TEntity, TKey> : ControllerBase
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    private static readonly HashSet<string> ReservedQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "page", "pageSize", "sort", "search",
    };

    public CrudController(ICrudService<TEntity, TKey> service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>The CRUD service for this entity.</summary>
    protected ICrudService<TEntity, TKey> Service { get; }

    /// <summary>Lists records with paging, sorting, free-text search and property filters.</summary>
    /// <remarks>
    /// Any query parameter that is not <c>page</c>, <c>pageSize</c>, <c>sort</c> or <c>search</c> is treated as a filter:
    ///
    ///     ?name=Laptop                 equals (default operator)
    ///     ?price:gte=10&amp;price:lt=100   range
    ///     ?name:contains=lap           case-insensitive contains (also startswith, endswith)
    ///     ?categoryId=null             null check (eq / ne only)
    ///     ?sort=name,-createdAtUtc     ascending name, then newest first
    ///     ?search=lap                  matches any text property
    ///
    /// Operators: eq, ne, gt, gte, lt, lte, contains, startswith, endswith.
    /// </remarks>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <param name="sort">Comma-separated property names; prefix with '-' for descending.</param>
    /// <param name="search">Case-insensitive text matched against every string property.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<PagedResult<TEntity>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = QueryParameters.DefaultPageSize,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var filters = FilterParser.Parse(
            Request.Query
                .Where(pair => !ReservedQueryKeys.Contains(pair.Key))
                .Select(pair => KeyValuePair.Create(pair.Key, pair.Value.ToString())));

        var parameters = new QueryParameters
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
            Search = search,
            Filters = filters,
        };

        return Ok(await Service.GetPagedAsync(parameters, cancellationToken));
    }

    /// <summary>Gets a single record by id.</summary>
    /// <param name="id">The record id.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TEntity>> GetById(TKey id, CancellationToken cancellationToken = default) =>
        Ok(await Service.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a record. Generated ids and audit fields are assigned by the server.</summary>
    /// <param name="entity">The record to create.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Create([FromBody] TEntity entity, CancellationToken cancellationToken = default)
    {
        var created = await Service.CreateAsync(entity, cancellationToken);
        var basePath = Request.Path.HasValue ? Request.Path.Value!.TrimEnd('/') : string.Empty;
        return Created($"{basePath}/{Uri.EscapeDataString(created.Id.ToString() ?? string.Empty)}", created);
    }

    /// <summary>Replaces every writable property of a record.</summary>
    /// <param name="id">The record id.</param>
    /// <param name="entity">The full replacement. Its id must be omitted or match the URL.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Update(TKey id, [FromBody] TEntity entity, CancellationToken cancellationToken = default) =>
        Ok(await Service.UpdateAsync(id, entity, cancellationToken));

    /// <summary>Changes only the supplied properties (JSON merge patch).</summary>
    /// <remarks>Example body: <c>{ "price": 9.99, "description": null }</c></remarks>
    /// <param name="id">The record id.</param>
    /// <param name="patch">Property names and new values.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpPatch("{id}")]
    [Consumes("application/json", "application/merge-patch+json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Patch(
        TKey id,
        [FromBody] Dictionary<string, JsonElement> patch,
        CancellationToken cancellationToken = default) =>
        Ok(await Service.PatchAsync(id, patch, cancellationToken));

    /// <summary>Deletes a record.</summary>
    /// <param name="id">The record id.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public virtual async Task<IActionResult> Delete(TKey id, CancellationToken cancellationToken = default)
    {
        await Service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
