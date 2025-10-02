namespace Backend.Domain.Events;

/// <summary>
/// Domain event raised when a page's title is changed.
/// </summary>
public class PageTitleChangedEvent : IDomainEvent
{
    public Guid PageId { get; }
    public Guid OrgId { get; }
    public string OldTitle { get; }
    public string NewTitle { get; }
    public Guid ChangedBy { get; }
    public DateTime OccurredAt { get; }

    public PageTitleChangedEvent(Guid pageId, Guid orgId, string oldTitle, string newTitle, Guid changedBy)
    {
        PageId = pageId;
        OrgId = orgId;
        OldTitle = oldTitle;
        NewTitle = newTitle;
        ChangedBy = changedBy;
        OccurredAt = DateTime.UtcNow;
    }
}
