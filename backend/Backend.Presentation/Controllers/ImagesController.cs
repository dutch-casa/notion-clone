using Backend.Application.UseCases.Images.UploadImage;
using Backend.Application.UseCases.Images.DeleteImage;
using Backend.Presentation.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly UploadImageHandler _uploadImageHandler;
    private readonly DeleteImageHandler _deleteImageHandler;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] AllowedContentTypes = { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp" };

    public ImagesController(
        UploadImageHandler uploadImageHandler,
        DeleteImageHandler deleteImageHandler)
    {
        _uploadImageHandler = uploadImageHandler;
        _deleteImageHandler = deleteImageHandler;
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
    /// Upload an image to a page
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequestDto request)
    {
        var userId = GetUserId();

        // Validation
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "No file provided"
            });
        }

        if (request.File.Length > MaxFileSize)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = $"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB"
            });
        }

        if (!AllowedContentTypes.Contains(request.File.ContentType.ToLower()))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = $"Invalid file type. Allowed types: {string.Join(", ", AllowedContentTypes)}"
            });
        }

        // Upload image
        var command = new UploadImageCommand(
            request.PageId,
            request.OrgId,
            request.File.FileName,
            request.File.OpenReadStream(),
            request.File.ContentType,
            request.File.Length,
            userId
        );

        var result = await _uploadImageHandler.HandleAsync(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete an image
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteImage(Guid id)
    {
        var userId = GetUserId();
        var command = new DeleteImageCommand(id, userId);
        await _deleteImageHandler.HandleAsync(command);
        return NoContent();
    }
}
