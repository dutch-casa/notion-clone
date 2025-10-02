namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when a block cannot be found.
/// </summary>
public class BlockNotFoundException : EntityNotFoundException
{
    public BlockNotFoundException(Guid blockId)
        : base("Block", blockId)
    {
    }
}
