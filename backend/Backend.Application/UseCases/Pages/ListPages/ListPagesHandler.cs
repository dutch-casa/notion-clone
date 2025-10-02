using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Pages.ListPages;

public class ListPagesHandler
{
    private readonly IPageRepository _pageRepository;
    private readonly IOrgRepository _orgRepository;

    public ListPagesHandler(
        IPageRepository pageRepository,
        IOrgRepository orgRepository)
    {
        _pageRepository = pageRepository;
        _orgRepository = orgRepository;
    }

    public async Task<ListPagesResult> HandleAsync(ListPagesQuery query)
    {
        // Verify user has access to organization
        var org = await _orgRepository.GetByIdWithMembersAsync(query.OrgId);

        if (org == null)
        {
            throw new InvalidOperationException($"Organization {query.OrgId} not found");
        }

        if (!org.Members.Any(m => m.UserId == query.UserId))
        {
            throw new UnauthorizedAccessException("User does not have access to this organization");
        }

        var pages = await _pageRepository.GetPagesByOrgIdAsync(query.OrgId);

        var pageSummaries = pages
            .Select(p => new PageSummary(
                p.Id,
                p.Title,
                p.CreatedBy,
                p.CreatedAt
            ))
            .ToList();

        return new ListPagesResult(pageSummaries);
    }
}
