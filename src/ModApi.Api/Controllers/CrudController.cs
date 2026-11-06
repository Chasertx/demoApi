using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModApi.Application.Abstractions;
using ModApi.Application.Querying;
using ModApi.Domain.Common;

namespace ModApi.Api.Controllers;

// One closed instance of this is registered per entity (see CrudControllerFeatureProvider).
// Subclass it if an entity needs extra endpoints; the generic one is then skipped for that entity.
[ApiController]
[Route("api/[controller]")]
public class CrudController<TEntity, TKey>(ICrudService<TEntity, TKey> service) : ControllerBase
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    private static readonly HashSet<string> ReservedQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "page", "pageSize", "sort", "search",
    };

    protected ICrudService<TEntity, TKey> Service { get; } = service;

    /// <summary>Lists records with paging, sorting, search and filters.</summary>
    /// <remarks>
    /// Any other query parameter is a filter, e.g. <c>name=Laptop</c>, <c>price:gte=10</c>,
    /// <c>name:contains=lap</c> or <c>categoryId=null</c>.
    /// Operators: eq, ne, gt, gte, lt, lte, contains, startswith, endswith.
    /// </remarks>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <param name="sort">Comma separated properties, prefix with '-' for descending.</param>
    /// <param name="search">Text to look for in any string property.</param>
    /// <param name="cancellationToken"></param>
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
        var filters = FilterParser.Parse(Request.Query
            .Where(q => !ReservedQueryKeys.Contains(q.Key))
            .Select(q => KeyValuePair.Create(q.Key, q.Value.ToString())));

        var result = await Service.GetPagedAsync(
            new QueryParameters { Page = page, PageSize = pageSize, Sort = sort, Search = search, Filters = filters },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Gets a record by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TEntity>> GetById(TKey id, CancellationToken cancellationToken = default) =>
        Ok(await Service.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a record. Ids and audit fields are set by the server.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Create([FromBody] TEntity entity, CancellationToken cancellationToken = default)
    {
        var created = await Service.CreateAsync(entity, cancellationToken);
        var path = Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        var id = Uri.EscapeDataString(created.Id.ToString() ?? string.Empty);

        return Created($"{path}/{id}", created);
    }

    /// <summary>Replaces a record.</summary>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public virtual async Task<ActionResult<TEntity>> Update(TKey id, [FromBody] TEntity entity, CancellationToken cancellationToken = default) =>
        Ok(await Service.UpdateAsync(id, entity, cancellationToken));

    /// <summary>Updates only the fields in the body, e.g. <c>{ "price": 9.99 }</c>.</summary>
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
