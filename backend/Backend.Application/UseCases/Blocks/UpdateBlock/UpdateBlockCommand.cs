namespace Backend.Application.UseCases.Blocks.UpdateBlock;

public record UpdateBlockCommand(
    Guid BlockId,
    string? Type,
    decimal? SortKey,
    string? Json,
    Guid UserId
);
