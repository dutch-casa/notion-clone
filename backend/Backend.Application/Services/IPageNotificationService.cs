namespace Backend.Application.Services;

/// <summary>
/// Service for publishing real-time page notifications to clients via SSE.
/// </summary>
public interface IPageNotificationService
{
    /// <summary>
    /// Publish a page notification to all subscribers in an organization.
    /// </summary>
    Task PublishPageNotificationAsync(
        Guid orgId,
        PageNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to page notifications for a specific organization.
    /// Returns an async stream of notifications.
    /// </summary>
    IAsyncEnumerable<PageNotification> SubscribeToPageNotificationsAsync(
        Guid orgId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Notification payload for page events.
/// </summary>
public class PageNotification
{
    public required string EventType { get; init; } // "PageCreated", "PageRenamed", "PageDeleted"
    public required Guid PageId { get; init; }
    public required Guid OrgId { get; init; }
    public required string Title { get; init; }
    public string? OldTitle { get; init; } // For rename events
    public required Guid ActorUserId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
