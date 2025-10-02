using Backend.Application.UseCases.Blocks.AddBlock;
using Backend.Application.UseCases.Blocks.UpdateBlock;
using Backend.Application.UseCases.Blocks.RemoveBlock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BlocksController : ControllerBase
{
    private readonly AddBlockHandler _addBlockHandler;
    private readonly UpdateBlockHandler _updateBlockHandler;
    private readonly RemoveBlockHandler _removeBlockHandler;

    public BlocksController(
        AddBlockHandler addBlockHandler,
        UpdateBlockHandler updateBlockHandler,
        RemoveBlockHandler removeBlockHandler)
    {
        _addBlockHandler = addBlockHandler;
        _updateBlockHandler = updateBlockHandler;
        _removeBlockHandler = removeBlockHandler;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// Add a new block to a page
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AddBlockResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddBlock([FromBody] AddBlockRequest request)
    {
        var userId = GetUserId();
        var command = new AddBlockCommand(
            request.PageId,
            request.SortKey,
            request.Type,
            request.ParentBlockId,
            request.Json,
            userId);
        var result = await _addBlockHandler.HandleAsync(command);

        return CreatedAtAction(
            nameof(AddBlock),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Update an existing block
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateBlock(Guid id, [FromBody] UpdateBlockRequest request)
    {
        var userId = GetUserId();
        var command = new UpdateBlockCommand(
            id,
            request.Type,
            request.SortKey,
            request.Json,
            userId);
        await _updateBlockHandler.HandleAsync(command);
        return NoContent();
    }

    /// <summary>
    /// Remove a block from a page
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveBlock(Guid id)
    {
        var userId = GetUserId();
        var command = new RemoveBlockCommand(id, userId);
        await _removeBlockHandler.HandleAsync(command);
        return NoContent();
    }
}

// DTOs
public record AddBlockRequest
{
    public required Guid PageId { get; init; }
    public required decimal SortKey { get; init; }
    public required string Type { get; init; }
    public Guid? ParentBlockId { get; init; }
    public string? Json { get; init; }
}

public record UpdateBlockRequest
{
    public string? Type { get; init; }
    public decimal? SortKey { get; init; }
    public string? Json { get; init; }
}
