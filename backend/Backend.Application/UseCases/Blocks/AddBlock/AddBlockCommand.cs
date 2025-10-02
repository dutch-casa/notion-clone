namespace Backend.Application.UseCases.Blocks.AddBlock;

public record AddBlockCommand(
    Guid PageId,
    decimal SortKey,
    string Type,
    Guid? ParentBlockId,
    string? Json,
    Guid UserId
);
