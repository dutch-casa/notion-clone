namespace Backend.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that you want other parts of the same domain to be aware of.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The date and time when the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}
