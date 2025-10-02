namespace Backend.Application.Services;

/// <summary>
/// Service for broadcasting invitation notifications to users in real-time
/// </summary>
public interface IInvitationNotificationService
{
    /// <summary>
    /// Publish an invitation created event to all connected clients for the invited user
    /// </summary>
    Task PublishInvitationCreatedAsync(Guid invitedUserId, InvitationNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to invitation notifications for a specific user
    /// Returns an async enumerable that yields notifications as they arrive
    /// </summary>
    IAsyncEnumerable<InvitationNotification> SubscribeToInvitationsAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Notification data sent when an invitation is created
/// </summary>
public record InvitationNotification(
    Guid InvitationId,
    Guid OrgId,
    string OrgName,
    Guid InviterUserId,
    string InviterName,
    string Role,
    DateTimeOffset CreatedAt
);
