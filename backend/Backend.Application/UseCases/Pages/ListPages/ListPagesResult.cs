namespace Backend.Application.UseCases.Pages.ListPages;

public record PageSummary(
    Guid Id,
    string Title,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

public record ListPagesResult(
    List<PageSummary> Pages
);
