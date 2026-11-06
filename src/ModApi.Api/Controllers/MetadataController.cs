using Microsoft.AspNetCore.Mvc;
using ModApi.Application.Entities;

namespace ModApi.Api.Controllers;

/// <summary>
/// Describes the exposed entities so a modular frontend can discover resources and build
/// tables and forms dynamically instead of hard-coding them.
/// </summary>
[ApiController]
[Route("api/metadata")]
public sealed class MetadataController : ControllerBase
{
    private readonly IEntityMetadataProvider _metadata;

    public MetadataController(IEntityMetadataProvider metadata)
    {
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>Lists every exposed entity with its route and property schema.</summary>
    [HttpGet("entities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<EntityMetadata>> GetEntities() => Ok(_metadata.GetAll());

    /// <summary>Describes a single entity.</summary>
    /// <param name="route">The entity route segment, e.g. <c>products</c>.</param>
    [HttpGet("entities/{route}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<EntityMetadata> GetEntity(string route) =>
        _metadata.Find(route) is { } metadata ? Ok(metadata) : NotFound();
}
