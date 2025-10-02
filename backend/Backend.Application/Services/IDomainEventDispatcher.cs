using Backend.Domain.Events;

namespace Backend.Application.Services;

/// <summary>
/// Service responsible for dispatching domain events to their respective handlers.
/// This abstraction allows the Infrastructure layer to remain decoupled from specific event handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a domain event to all registered handlers.
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
