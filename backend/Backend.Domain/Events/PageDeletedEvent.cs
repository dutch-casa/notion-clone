namespace Backend.Domain.Events;

/// <summary>
/// Domain event raised when a page is deleted.
/// </summary>
public class PageDeletedEvent : IDomainEvent
{
    public Guid PageId { get; }
    public Guid OrgId { get; }
    public string Title { get; }
    public Guid DeletedBy { get; }
    public DateTime OccurredAt { get; }

    public PageDeletedEvent(Guid pageId, Guid orgId, string title, Guid deletedBy)
    {
        PageId = pageId;
        OrgId = orgId;
        Title = title;
        DeletedBy = deletedBy;
        OccurredAt = DateTime.UtcNow;
    }
}
