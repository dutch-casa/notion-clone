using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Pages.DeletePage;

public class DeletePageHandler
{
    private readonly IPageRepository _pageRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePageHandler(
        IPageRepository pageRepository,
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeletePageCommand command)
    {
        var page = await _pageRepository.GetByIdAsync(command.PageId);
        if (page == null)
        {
            throw new InvalidOperationException($"Page {command.PageId} not found");
        }

        // Verify user has access to organization
        var org = await _orgRepository.GetByIdWithMembersAsync(page.OrgId);

        if (org == null)
        {
            throw new InvalidOperationException($"Organization {page.OrgId} not found");
        }

        if (!org.Members.Any(m => m.UserId == command.UserId))
        {
            throw new UnauthorizedAccessException("User does not have access to this organization");
        }

        page.Delete(command.UserId);
        _pageRepository.Remove(page);
        await _unitOfWork.SaveChangesAsync();
    }
}
