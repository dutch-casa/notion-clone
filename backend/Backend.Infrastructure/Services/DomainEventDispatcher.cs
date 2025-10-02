using Backend.Application.Services;
using Backend.Domain.Events;
using Backend.Infrastructure.EventHandlers;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of domain event dispatcher.
/// Uses a registry of handlers to dispatch events without tight coupling.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly PageCreatedEventHandler _pageCreatedHandler;
    private readonly PageTitleChangedEventHandler _pageTitleChangedHandler;
    private readonly PageDeletedEventHandler _pageDeletedHandler;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        PageCreatedEventHandler pageCreatedHandler,
        PageTitleChangedEventHandler pageTitleChangedHandler,
        PageDeletedEventHandler pageDeletedHandler,
        ILogger<DomainEventDispatcher> logger)
    {
        _pageCreatedHandler = pageCreatedHandler;
        _pageTitleChangedHandler = pageTitleChangedHandler;
        _pageDeletedHandler = pageDeletedHandler;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Dispatching domain event: {EventType}", domainEvent.GetType().Name);

        // Pattern matching for event routing
        // As new events are added, add new cases here
        switch (domainEvent)
        {
            case PageCreatedEvent pageCreated:
                await _pageCreatedHandler.HandleAsync(pageCreated, cancellationToken);
                break;
            case PageTitleChangedEvent pageTitleChanged:
                await _pageTitleChangedHandler.HandleAsync(pageTitleChanged, cancellationToken);
                break;
            case PageDeletedEvent pageDeleted:
                await _pageDeletedHandler.HandleAsync(pageDeleted, cancellationToken);
                break;
            default:
                _logger.LogWarning("No handler registered for event type: {EventType}", domainEvent.GetType().Name);
                break;
        }
    }
}
