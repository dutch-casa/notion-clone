namespace Backend.Application.UseCases.Pages.ListPages;

public record ListPagesQuery(
    Guid OrgId,
    Guid UserId
);
