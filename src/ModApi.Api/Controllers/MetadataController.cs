using Microsoft.AspNetCore.Mvc;
using ModApi.Application.Entities;

namespace ModApi.Api.Controllers;

[ApiController]
[Route("api/metadata")]
public sealed class MetadataController(IEntityMetadataProvider metadata) : ControllerBase
{
    /// <summary>Lists every exposed entity with its route and fields.</summary>
    [HttpGet("entities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<EntityMetadata>> GetEntities() => Ok(metadata.GetAll());

    /// <summary>Describes a single entity, e.g. <c>products</c>.</summary>
    [HttpGet("entities/{route}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<EntityMetadata> GetEntity(string route) =>
        metadata.Find(route) is { } entity ? Ok(entity) : NotFound();
}
