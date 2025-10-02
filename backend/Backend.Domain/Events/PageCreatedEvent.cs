namespace Backend.Domain.Events;

/// <summary>
/// Domain event raised when a new page is created.
/// </summary>
public class PageCreatedEvent : IDomainEvent
{
    public Guid PageId { get; }
    public Guid OrgId { get; }
    public Guid CreatedBy { get; }
    public string Title { get; }
    public DateTime OccurredAt { get; }

    public PageCreatedEvent(Guid pageId, Guid orgId, Guid createdBy, string title)
    {
        PageId = pageId;
        OrgId = orgId;
        CreatedBy = createdBy;
        Title = title;
        OccurredAt = DateTime.UtcNow;
    }
}
