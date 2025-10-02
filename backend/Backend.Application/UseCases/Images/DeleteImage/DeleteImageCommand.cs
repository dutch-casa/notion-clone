namespace Backend.Application.UseCases.Images.DeleteImage;

public record DeleteImageCommand(
    Guid ImageId,
    Guid UserId
);
