using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.RemoveMember;

public class RemoveMemberHandler
{
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMemberHandler(
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        RemoveMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get organization with members
        var org = await _orgRepository.GetByIdWithMembersAsync(command.OrgId, cancellationToken);

        if (org == null)
        {
            throw new InvalidOperationException($"Organization {command.OrgId} not found");
        }

        // Verify requesting user is owner or admin
        var requestingMember = org.Members.FirstOrDefault(m => m.UserId == command.RequestingUserId);
        if (requestingMember == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this organization");
        }

        if (!requestingMember.Role.CanInviteMembers)
        {
            throw new UnauthorizedAccessException("Only owners and admins can remove members");
        }

        // Remove member (domain logic prevents removing owner)
        org.RemoveMember(command.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
