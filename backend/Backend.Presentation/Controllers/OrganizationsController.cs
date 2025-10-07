using Backend.Application.UseCases.Organizations.CreateOrganization;
using Backend.Application.UseCases.Organizations.ListOrganizations;
using Backend.Application.UseCases.Organizations.GetOrganization;
using Backend.Application.UseCases.Organizations.InviteMember;
using Backend.Application.UseCases.Organizations.RemoveMember;
using Backend.Application.UseCases.Organizations.UpdateMemberRole;
using Backend.Application.UseCases.Organizations.CreateInvitation;
using Backend.Application.UseCases.Organizations.ListInvitations;
using Backend.Application.UseCases.Organizations.AcceptInvitation;
using Backend.Application.UseCases.Organizations.DeclineInvitation;
using Backend.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly CreateOrganizationHandler _createOrgHandler;
    private readonly ListOrganizationsHandler _listOrgsHandler;
    private readonly GetOrganizationHandler _getOrgHandler;
    private readonly InviteMemberHandler _inviteMemberHandler;
    private readonly RemoveMemberHandler _removeMemberHandler;
    private readonly UpdateMemberRoleHandler _updateMemberRoleHandler;
    private readonly CreateInvitationHandler _createInvitationHandler;
    private readonly CreateInvitationByEmailHandler _createInvitationByEmailHandler;
    private readonly ListInvitationsHandler _listInvitationsHandler;
    private readonly AcceptInvitationHandler _acceptInvitationHandler;
    private readonly DeclineInvitationHandler _declineInvitationHandler;
    private readonly IInvitationNotificationService _notificationService;

    public OrganizationsController(
        CreateOrganizationHandler createOrgHandler,
        ListOrganizationsHandler listOrgsHandler,
        GetOrganizationHandler getOrgHandler,
        InviteMemberHandler inviteMemberHandler,
        RemoveMemberHandler removeMemberHandler,
        UpdateMemberRoleHandler updateMemberRoleHandler,
        CreateInvitationHandler createInvitationHandler,
        CreateInvitationByEmailHandler createInvitationByEmailHandler,
        ListInvitationsHandler listInvitationsHandler,
        AcceptInvitationHandler acceptInvitationHandler,
        DeclineInvitationHandler declineInvitationHandler,
        IInvitationNotificationService notificationService)
    {
        _createOrgHandler = createOrgHandler;
        _listOrgsHandler = listOrgsHandler;
        _getOrgHandler = getOrgHandler;
        _inviteMemberHandler = inviteMemberHandler;
        _removeMemberHandler = removeMemberHandler;
        _updateMemberRoleHandler = updateMemberRoleHandler;
        _createInvitationHandler = createInvitationHandler;
        _createInvitationByEmailHandler = createInvitationByEmailHandler;
        _listInvitationsHandler = listInvitationsHandler;
        _acceptInvitationHandler = acceptInvitationHandler;
        _declineInvitationHandler = declineInvitationHandler;
        _notificationService = notificationService;
    }

    private Guid GetUserId()
    {
        // Try standard JWT "sub" claim first, then fallback to ClaimTypes.NameIdentifier
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// List all organizations the authenticated user is a member of
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrganizations(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new ListOrganizationsCommand(userId);
        var result = await _listOrgsHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrganizationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new CreateOrganizationCommand(request.Name, userId);
        var result = await _createOrgHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetOrganization),
            new { id = result.OrgId },
            result);
    }

    /// <summary>
    /// Get organization details by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetOrganizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrganization(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new GetOrganizationCommand(id, userId);
        var result = await _getOrgHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Invite a user to the organization
    /// </summary>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(InviteMemberResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteMember(
        Guid id,
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = GetUserId();
        var command = new InviteMemberCommand(id, request.UserId, request.Role, requestingUserId);
        var result = await _inviteMemberHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetOrganization), new { id }, result);
    }

    /// <summary>
    /// Remove a member from the organization
    /// </summary>
    [HttpDelete("{id}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var requestingUserId = GetUserId();
        var command = new RemoveMemberCommand(id, userId, requestingUserId);
        await _removeMemberHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Update a member's role in the organization
    /// </summary>
    [HttpPatch("{id}/members/{memberId}")]
    [ProducesResponseType(typeof(UpdateMemberRoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid id,
        Guid memberId,
        [FromBody] UpdateMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = GetUserId();
        var command = new UpdateMemberRoleCommand(id, memberId, request.Role, requestingUserId);
        var result = await _updateMemberRoleHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create an invitation to join the organization by email
    /// </summary>
    [HttpPost("{id}/invitations/by-email")]
    [ProducesResponseType(typeof(CreateInvitationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateInvitationByEmail(
        Guid id,
        [FromBody] CreateInvitationByEmailRequest request,
        CancellationToken cancellationToken)
    {
        var inviterUserId = GetUserId();
        var command = new CreateInvitationByEmailCommand(id, request.Email, request.Role, inviterUserId);
        var result = await _createInvitationByEmailHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(CreateInvitationByEmail), new { id }, result);
    }

    /// <summary>
    /// Create an invitation to join the organization
    /// </summary>
    [HttpPost("{id}/invitations")]
    [ProducesResponseType(typeof(CreateInvitationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateInvitation(
        Guid id,
        [FromBody] CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var inviterUserId = GetUserId();
        var command = new CreateInvitationCommand(id, request.InvitedUserId, request.Role, inviterUserId);
        var result = await _createInvitationHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(CreateInvitation), new { id }, result);
    }

    /// <summary>
    /// List pending invitations for the authenticated user
    /// </summary>
    [HttpGet("invitations")]
    [ProducesResponseType(typeof(IEnumerable<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvitations(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new ListInvitationsCommand(userId);
        var result = await _listInvitationsHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Accept an invitation to join an organization
    /// </summary>
    [HttpPost("invitations/{invitationId}/accept")]
    [ProducesResponseType(typeof(AcceptInvitationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvitation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new AcceptInvitationCommand(invitationId, userId);
        var result = await _acceptInvitationHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Decline an invitation to join an organization
    /// </summary>
    [HttpPost("invitations/{invitationId}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineInvitation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new DeclineInvitationCommand(invitationId, userId);
        await _declineInvitationHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Server-Sent Events endpoint for real-time invitation notifications
    /// </summary>
    [HttpGet("invitations/stream")]
    [Microsoft.AspNetCore.Cors.EnableCors("AllowFrontend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task StreamInvitationNotifications(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        // Disable response buffering for SSE
        var bufferingFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no"); // Disable nginx buffering

        // Send initial comment to establish connection
        await Response.WriteAsync($": Connected to invitation notifications\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var notification in _notificationService.SubscribeToInvitationsAsync(userId, cancellationToken))
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

public record CreateOrganizationRequest(string Name);
public record InviteMemberRequest(Guid UserId, string Role);
public record UpdateMemberRoleRequest(string Role);
public record CreateInvitationRequest(Guid InvitedUserId, string Role);
public record CreateInvitationByEmailRequest(string Email, string Role);
