namespace Backend.Application.UseCases.Blocks.RemoveBlock;

public record RemoveBlockCommand(
    Guid BlockId,
    Guid UserId
);
