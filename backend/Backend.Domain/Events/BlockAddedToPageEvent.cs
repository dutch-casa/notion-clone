namespace Backend.Domain.Events;

/// <summary>
/// Domain event raised when a block is added to a page.
/// </summary>
public class BlockAddedToPageEvent : IDomainEvent
{
    public Guid PageId { get; }
    public Guid BlockId { get; }
    public string BlockType { get; }
    public DateTime OccurredAt { get; }

    public BlockAddedToPageEvent(Guid pageId, Guid blockId, string blockType)
    {
        PageId = pageId;
        BlockId = blockId;
        BlockType = blockType;
        OccurredAt = DateTime.UtcNow;
    }
}
