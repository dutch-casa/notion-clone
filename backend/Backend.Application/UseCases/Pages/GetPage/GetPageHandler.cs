using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Pages.GetPage;

public class GetPageHandler
{
    private readonly IPageRepository _pageRepository;
    private readonly IOrgRepository _orgRepository;

    public GetPageHandler(
        IPageRepository pageRepository,
        IOrgRepository orgRepository)
    {
        _pageRepository = pageRepository;
        _orgRepository = orgRepository;
    }

    public async Task<GetPageResult> HandleAsync(GetPageQuery query)
    {
        var page = await _pageRepository.GetByIdWithBlocksAsync(query.PageId);

        if (page == null)
        {
            throw new InvalidOperationException($"Page {query.PageId} not found");
        }

        // Verify user has access to organization
        var org = await _orgRepository.GetByIdWithMembersAsync(page.OrgId);

        if (org == null)
        {
            throw new InvalidOperationException($"Organization {page.OrgId} not found");
        }

        if (!org.Members.Any(m => m.UserId == query.UserId))
        {
            throw new UnauthorizedAccessException("User does not have access to this organization");
        }

        var blocks = page.Blocks
            .OrderBy(b => b.SortKey)
            .Select(b => new BlockDto(
                b.Id,
                b.PageId,
                b.ParentBlockId,
                b.SortKey.Value,
                b.Type.Value,
                b.Json
            ))
            .ToList();

        return new GetPageResult(
            page.Id,
            page.OrgId,
            page.Title,
            page.CreatedBy,
            page.CreatedAt,
            blocks
        );
    }
}
