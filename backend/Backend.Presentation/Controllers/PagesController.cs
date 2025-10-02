using Backend.Application.UseCases.Pages.CreatePage;
using Backend.Application.UseCases.Pages.GetPage;
using Backend.Application.UseCases.Pages.ListPages;
using Backend.Application.UseCases.Pages.UpdatePageTitle;
using Backend.Application.UseCases.Pages.DeletePage;
using Backend.Application.Services;
using Backend.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagesController : ControllerBase
{
    private readonly CreatePageHandler _createPageHandler;
    private readonly GetPageHandler _getPageHandler;
    private readonly ListPagesHandler _listPagesHandler;
    private readonly UpdatePageTitleHandler _updatePageTitleHandler;
    private readonly DeletePageHandler _deletePageHandler;
    private readonly IPageNotificationService _pageNotificationService;
    private readonly IOrgRepository _orgRepository;

    public PagesController(
        CreatePageHandler createPageHandler,
        GetPageHandler getPageHandler,
        ListPagesHandler listPagesHandler,
        UpdatePageTitleHandler updatePageTitleHandler,
        DeletePageHandler deletePageHandler,
        IPageNotificationService pageNotificationService,
        IOrgRepository orgRepository)
    {
        _createPageHandler = createPageHandler;
        _getPageHandler = getPageHandler;
        _listPagesHandler = listPagesHandler;
        _updatePageTitleHandler = updatePageTitleHandler;
        _deletePageHandler = deletePageHandler;
        _pageNotificationService = pageNotificationService;
        _orgRepository = orgRepository;
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
    /// List all pages in an organization
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListPagesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListPages([FromQuery] Guid orgId)
    {
        var userId = GetUserId();
        var query = new ListPagesQuery(orgId, userId);
        var result = await _listPagesHandler.HandleAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Get page details by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetPageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPage(Guid id)
    {
        var userId = GetUserId();
        var query = new GetPageQuery(id, userId);
        var result = await _getPageHandler.HandleAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new page
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePageResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request)
    {
        var userId = GetUserId();
        var command = new CreatePageCommand(request.OrgId, request.Title, userId);
        var result = await _createPageHandler.HandleAsync(command);

        return CreatedAtAction(
            nameof(GetPage),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Update page title
    /// </summary>
    [HttpPatch("{id}/title")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePageTitle(Guid id, [FromBody] UpdatePageTitleRequest request)
    {
        var userId = GetUserId();
        var command = new UpdatePageTitleCommand(id, request.Title, userId);
        await _updatePageTitleHandler.HandleAsync(command);
        return NoContent();
    }

    /// <summary>
    /// Delete a page
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeletePage(Guid id)
    {
        var userId = GetUserId();
        var command = new DeletePageCommand(id, userId);
        await _deletePageHandler.HandleAsync(command);
        return NoContent();
    }

    /// <summary>
    /// Server-Sent Events endpoint for real-time page notifications
    /// </summary>
    [HttpGet("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task StreamPageNotifications([FromQuery] Guid orgId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        // Verify user has access to organization
        var org = await _orgRepository.GetByIdWithMembersAsync(orgId);
        if (org == null)
        {
            throw new InvalidOperationException($"Organization {orgId} not found");
        }

        if (!org.Members.Any(m => m.UserId == userId))
        {
            throw new UnauthorizedAccessException("User does not have access to this organization");
        }

        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        // Send initial comment to establish connection
        await Response.WriteAsync($": Connected to page notifications for org {orgId}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var notification in _pageNotificationService.SubscribeToPageNotificationsAsync(orgId, cancellationToken))
        {
            // Format as SSE data
            var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}

// DTOs
public record CreatePageRequest
{
    public required Guid OrgId { get; init; }
    public required string Title { get; init; }
}

public record UpdatePageTitleRequest
{
    public required string Title { get; init; }
}
