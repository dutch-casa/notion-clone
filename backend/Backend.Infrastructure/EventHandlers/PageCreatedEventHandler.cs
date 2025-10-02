using Backend.Application.Services;
using Backend.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.EventHandlers;

/// <summary>
/// Handles PageCreatedEvent and publishes notifications to subscribed clients.
/// </summary>
public class PageCreatedEventHandler
{
    private readonly IPageNotificationService _notificationService;
    private readonly ILogger<PageCreatedEventHandler> _logger;

    public PageCreatedEventHandler(
        IPageNotificationService notificationService,
        ILogger<PageCreatedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PageCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling PageCreatedEvent: PageId={PageId}, OrgId={OrgId}",
            domainEvent.PageId,
            domainEvent.OrgId);

        var notification = new PageNotification
        {
            EventType = "PageCreated",
            PageId = domainEvent.PageId,
            OrgId = domainEvent.OrgId,
            Title = domainEvent.Title,
            ActorUserId = domainEvent.CreatedBy,
            Timestamp = domainEvent.OccurredAt
        };

        await _notificationService.PublishPageNotificationAsync(
            domainEvent.OrgId,
            notification,
            cancellationToken);
    }
}
