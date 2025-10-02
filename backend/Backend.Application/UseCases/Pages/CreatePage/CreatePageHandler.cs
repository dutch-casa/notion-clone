using Backend.Domain.Aggregates;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Pages.CreatePage;

public class CreatePageHandler
{
    private readonly IOrgRepository _orgRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePageHandler(
        IOrgRepository orgRepository,
        IPageRepository pageRepository,
        IUnitOfWork unitOfWork)
    {
        _orgRepository = orgRepository;
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePageResult> HandleAsync(CreatePageCommand command)
    {
        // Verify user has access to organization
        var org = await _orgRepository.GetByIdWithMembersAsync(command.OrgId);
        if (org == null)
        {
            throw new InvalidOperationException($"Organization {command.OrgId} not found");
        }

        // Authorization: User must be a member of the organization
        if (!org.Members.Any(m => m.UserId == command.UserId))
        {
            throw new UnauthorizedAccessException($"User {command.UserId} is not a member of organization {command.OrgId}");
        }

        // Create page
        var page = new Page(command.OrgId, command.Title, command.UserId);

        await _pageRepository.AddAsync(page);
        await _unitOfWork.SaveChangesAsync();

        return new CreatePageResult(
            page.Id,
            page.OrgId,
            page.Title,
            page.CreatedBy,
            page.CreatedAt
        );
    }
}
