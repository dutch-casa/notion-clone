using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Organizations.InviteMember;

public class InviteMemberHandler
{
    private readonly IOrgRepository _orgRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InviteMemberHandler(
        IOrgRepository orgRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _orgRepository = orgRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InviteMemberResult> HandleAsync(
        InviteMemberCommand command,
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
            throw new UnauthorizedAccessException("Only owners and admins can invite members");
        }

        // Verify user exists
        var userExists = await _userRepository.ExistsAsync(command.UserId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException($"User {command.UserId} does not exist");
        }

        // Add member
        var role = OrgRole.Create(command.Role);
        var member = org.AddMember(command.UserId, role);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new InviteMemberResult(
            member.Id,
            member.UserId,
            member.Role.Value,
            member.JoinedAt);
    }
}
