namespace Backend.Application.UseCases.Pages.CreatePage;

public record CreatePageResult(
    Guid Id,
    Guid OrgId,
    string Title,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);
