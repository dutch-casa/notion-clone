using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Blocks.UpdateBlock;

public class UpdateBlockHandler
{
    private readonly IBlockRepository _blockRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBlockHandler(
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

    public async Task HandleAsync(UpdateBlockCommand command)
    {
        // First, find which page the block belongs to
        // We still need BlockRepository for read operations to find the PageId
        var block = await _blockRepository.GetByIdAsync(command.BlockId);

        if (block == null)
        {
            throw new InvalidOperationException($"Block {command.BlockId} not found");
        }

        // Load the page with blocks to respect aggregate boundary
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

        // Parse value objects if provided
        BlockType? blockType = command.Type != null ? BlockType.Create(command.Type) : null;
        SortKey? sortKey = command.SortKey.HasValue ? SortKey.Create(command.SortKey.Value) : null;

        // Update through aggregate root to maintain aggregate boundary
        page.UpdateBlock(command.BlockId, blockType, sortKey, command.Json);

        await _unitOfWork.SaveChangesAsync();
    }
}
