using Backend.Application.Services;
using Backend.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.EventHandlers;

/// <summary>
/// Handles PageDeletedEvent and publishes notifications to subscribed clients.
/// </summary>
public class PageDeletedEventHandler
{
    private readonly IPageNotificationService _notificationService;
    private readonly ILogger<PageDeletedEventHandler> _logger;

    public PageDeletedEventHandler(
        IPageNotificationService notificationService,
        ILogger<PageDeletedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PageDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling PageDeletedEvent: PageId={PageId}, OrgId={OrgId}",
            domainEvent.PageId,
            domainEvent.OrgId);

        var notification = new PageNotification
        {
            EventType = "PageDeleted",
            PageId = domainEvent.PageId,
            OrgId = domainEvent.OrgId,
            Title = domainEvent.Title,
            ActorUserId = domainEvent.DeletedBy,
            Timestamp = domainEvent.OccurredAt
        };

        await _notificationService.PublishPageNotificationAsync(
            domainEvent.OrgId,
            notification,
            cancellationToken);
    }
}
