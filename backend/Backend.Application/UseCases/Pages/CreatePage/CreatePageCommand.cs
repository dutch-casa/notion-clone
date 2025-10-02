namespace Backend.Application.UseCases.Pages.CreatePage;

public record CreatePageCommand(
    Guid OrgId,
    string Title,
    Guid UserId
);
