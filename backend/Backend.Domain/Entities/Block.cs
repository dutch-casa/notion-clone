using Backend.Domain.Aggregates;
using Backend.Domain.ValueObjects;

namespace Backend.Domain.Entities;

/// <summary>
/// Block entity representing a block of content within a page.
/// Part of Documents bounded context.
/// </summary>
public class Block
{
    public Guid Id { get; private set; }
    public Guid PageId { get; private set; }
    public Guid? ParentBlockId { get; private set; }
    public SortKey SortKey { get; private set; }
    public BlockType Type { get; private set; }
    public string? Json { get; private set; }

    public Block(Guid pageId, SortKey sortKey, BlockType type, Guid? parentBlockId, string? json)
    {
        if (pageId == Guid.Empty)
        {
            throw new ArgumentException("PageId cannot be empty", nameof(pageId));
        }

        Id = Guid.NewGuid();
        PageId = pageId;
        SortKey = sortKey;
        Type = type;
        ParentBlockId = parentBlockId;
        Json = json;
    }

    public void UpdateType(BlockType newType)
    {
        Type = newType;
    }

    public void UpdateSortKey(SortKey newSortKey)
    {
        SortKey = newSortKey;
    }

    public void UpdateJson(string? json)
    {
        Json = json;
    }

    // EF Core constructor
    private Block() { }
}
