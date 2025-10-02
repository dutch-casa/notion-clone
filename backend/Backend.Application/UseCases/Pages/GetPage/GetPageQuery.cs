namespace Backend.Application.UseCases.Pages.GetPage;

public record GetPageQuery(
    Guid PageId,
    Guid UserId
);
