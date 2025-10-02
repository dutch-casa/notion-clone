namespace Backend.Application.UseCases.Pages.UpdatePageTitle;

public record UpdatePageTitleCommand(
    Guid PageId,
    string NewTitle,
    Guid UserId
);
