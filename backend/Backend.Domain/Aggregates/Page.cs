using Backend.Domain.Common;
using Backend.Domain.Entities;
using Backend.Domain.Events;
using Backend.Domain.ValueObjects;

namespace Backend.Domain.Aggregates;

/// <summary>
/// Page aggregate root representing a collaborative document page.
/// Part of Documents bounded context.
/// </summary>
public class Page : AggregateRoot
{
    private List<Block> _blocks = new();

    public Guid Id { get; private set; }
    public Guid OrgId { get; private set; }
    public string Title { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<Block> Blocks => _blocks;

    public Page(Guid orgId, string title, Guid createdBy)
    {
        if (orgId == Guid.Empty)
        {
            throw new ArgumentException("OrgId cannot be empty", nameof(orgId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("CreatedBy cannot be empty", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrgId = orgId;
        Title = title.Trim();
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;

        // Raise domain event
        AddDomainEvent(new PageCreatedEvent(Id, OrgId, CreatedBy, Title));
    }

    public void ChangeTitle(string newTitle, Guid changedBy)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));
        }

        var oldTitle = Title;
        Title = newTitle.Trim();

        // Raise domain event
        AddDomainEvent(new PageTitleChangedEvent(Id, OrgId, oldTitle, Title, changedBy));
    }

    public void Delete(Guid deletedBy)
    {
        // Raise domain event
        AddDomainEvent(new PageDeletedEvent(Id, OrgId, Title, deletedBy));
    }

    public Block AddBlock(SortKey sortKey, BlockType type, Guid? parentBlockId, string? json)
    {
        var block = new Block(Id, sortKey, type, parentBlockId, json);
        _blocks.Add(block);

        // Raise domain event
        AddDomainEvent(new BlockAddedToPageEvent(Id, block.Id, type.Value));

        return block;
    }

    public void UpdateBlock(Guid blockId, BlockType? newType, SortKey? newSortKey, string? newJson)
    {
        var block = _blocks.FirstOrDefault(b => b.Id == blockId);
        if (block == null)
        {
            throw new InvalidOperationException($"Block {blockId} not found on page {Id}");
        }

        if (newType != null)
        {
            block.UpdateType(newType.Value);
        }

        if (newSortKey != null)
        {
            block.UpdateSortKey(newSortKey.Value);
        }

        if (newJson != null)
        {
            block.UpdateJson(newJson);
        }

        // Could raise domain event here if needed: BlockUpdatedEvent
    }

    public void RemoveBlock(Guid blockId)
    {
        var block = _blocks.FirstOrDefault(b => b.Id == blockId);
        if (block == null)
        {
            throw new InvalidOperationException($"Block {blockId} not found on page {Id}");
        }

        _blocks.Remove(block);
    }

    public Block? GetBlock(Guid blockId)
    {
        return _blocks.FirstOrDefault(b => b.Id == blockId);
    }

    // EF Core constructor
    private Page() { }
}
