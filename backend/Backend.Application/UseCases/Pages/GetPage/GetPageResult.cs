namespace Backend.Application.UseCases.Pages.GetPage;

public record BlockDto(
    Guid Id,
    Guid PageId,
    Guid? ParentBlockId,
    decimal SortKey,
    string Type,
    string? Json
);

public record GetPageResult(
    Guid Id,
    Guid OrgId,
    string Title,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    List<BlockDto> Blocks
);
