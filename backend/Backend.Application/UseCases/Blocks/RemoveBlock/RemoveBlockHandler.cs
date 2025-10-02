using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Blocks.RemoveBlock;

public class RemoveBlockHandler
{
    private readonly IBlockRepository _blockRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveBlockHandler(
        IBlockRepository blockRepository,
        IPageRepository pageRepository,
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _blockRepository = blockRepository;
        _pageRepository = pageRepository;
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(RemoveBlockCommand command)
    {
        var block = await _blockRepository.GetByIdAsync(command.BlockId);

        if (block == null)
        {
            throw new InvalidOperationException($"Block {command.BlockId} not found");
        }

        // Load the page (respects aggregate boundary - Block only references Page by ID)
        var page = await _pageRepository.GetByIdWithBlocksAsync(block.PageId);

        if (page == null)
        {
            throw new InvalidOperationException($"Page {block.PageId} not found");
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

        page.RemoveBlock(command.BlockId);

        await _unitOfWork.SaveChangesAsync();
    }
}
