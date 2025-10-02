namespace Backend.Application.UseCases.Blocks.AddBlock;

public record AddBlockResult(
    Guid Id,
    Guid PageId,
    Guid? ParentBlockId,
    decimal SortKey,
    string Type,
    string? Json
);
