using Backend.Application.Services;
using Backend.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.EventHandlers;

/// <summary>
/// Handles PageTitleChangedEvent and publishes notifications to subscribed clients.
/// </summary>
public class PageTitleChangedEventHandler
{
    private readonly IPageNotificationService _notificationService;
    private readonly ILogger<PageTitleChangedEventHandler> _logger;

    public PageTitleChangedEventHandler(
        IPageNotificationService notificationService,
        ILogger<PageTitleChangedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PageTitleChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling PageTitleChangedEvent: PageId={PageId}, OldTitle={OldTitle}, NewTitle={NewTitle}",
            domainEvent.PageId,
            domainEvent.OldTitle,
            domainEvent.NewTitle);

        var notification = new PageNotification
        {
            EventType = "PageTitleChanged",
            PageId = domainEvent.PageId,
            OrgId = domainEvent.OrgId,
            Title = domainEvent.NewTitle,
            OldTitle = domainEvent.OldTitle,
            ActorUserId = domainEvent.ChangedBy,
            Timestamp = domainEvent.OccurredAt
        };

        await _notificationService.PublishPageNotificationAsync(
            domainEvent.OrgId,
            notification,
            cancellationToken);
    }
}
