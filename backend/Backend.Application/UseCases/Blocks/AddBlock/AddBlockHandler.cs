using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Blocks.AddBlock;

public class AddBlockHandler
{
    private readonly IPageRepository _pageRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddBlockHandler(
        IPageRepository pageRepository,
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddBlockResult> HandleAsync(AddBlockCommand command)
    {
        var page = await _pageRepository.GetByIdWithBlocksAsync(command.PageId);

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

        var sortKey = SortKey.Create(command.SortKey);
        var blockType = BlockType.Create(command.Type);

        var block = page.AddBlock(sortKey, blockType, command.ParentBlockId, command.Json);

        await _unitOfWork.SaveChangesAsync();

        return new AddBlockResult(
            block.Id,
            block.PageId,
            block.ParentBlockId,
            block.SortKey.Value,
            block.Type.Value,
            block.Json
        );
    }
}
