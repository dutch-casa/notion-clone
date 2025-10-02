namespace Backend.Application.UseCases.Pages.DeletePage;

public record DeletePageCommand(
    Guid PageId,
    Guid UserId
);
