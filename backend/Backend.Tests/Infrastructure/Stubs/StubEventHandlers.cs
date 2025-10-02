using System.Runtime.CompilerServices;
using Backend.Application.Services;
using Backend.Domain.Events;
using Backend.Infrastructure.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Infrastructure.Stubs;

/// <summary>
/// Stub (NullObject) event handlers for testing.
/// These implement the do-nothing pattern to satisfy dependencies without side effects.
/// Following Clean Architecture: tests should not depend on real infrastructure implementations.
/// </summary>
public class StubDomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Do nothing - stub for testing
        return Task.CompletedTask;
    }
}

public class StubPageCreatedEventHandler : PageCreatedEventHandler
{
    public StubPageCreatedEventHandler()
        : base(new StubPageNotificationService(), NullLogger<PageCreatedEventHandler>.Instance)
    {
    }

    // Inherits HandleAsync from base class - calls stub notification service which does nothing
}

public class StubPageTitleChangedEventHandler : PageTitleChangedEventHandler
{
    public StubPageTitleChangedEventHandler()
        : base(new StubPageNotificationService(), NullLogger<PageTitleChangedEventHandler>.Instance)
    {
    }

    // Inherits HandleAsync from base class - calls stub notification service which does nothing
}

public class StubPageDeletedEventHandler : PageDeletedEventHandler
{
    public StubPageDeletedEventHandler()
        : base(new StubPageNotificationService(), NullLogger<PageDeletedEventHandler>.Instance)
    {
    }

    // Inherits HandleAsync from base class - calls stub notification service which does nothing
}

public class StubPageNotificationService : IPageNotificationService
{
    public Task PublishPageNotificationAsync(Guid orgId, PageNotification notification, CancellationToken cancellationToken = default)
    {
        // Do nothing - stub for testing
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<PageNotification> SubscribeToPageNotificationsAsync(Guid orgId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Return empty stream - stub for testing
        yield break;
    }
}
