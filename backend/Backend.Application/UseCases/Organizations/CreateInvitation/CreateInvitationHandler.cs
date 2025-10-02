using Backend.Domain.Repositories;
using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Organizations.CreateInvitation;

public class CreateInvitationHandler
{
    private readonly IOrgRepository _orgRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInvitationHandler(
        IOrgRepository orgRepository,
        IUserRepository userRepository,
        IInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork)
    {
        _orgRepository = orgRepository;
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateInvitationResult> HandleAsync(
        CreateInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verify organization exists
        var org = await _orgRepository.GetByIdWithMembersAsync(command.OrgId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found");

        // Authorization check - only owners and admins can invite
        var inviterMember = org.Members.FirstOrDefault(m => m.UserId == command.InviterUserId)
            ?? throw new UnauthorizedAccessException("Only organization members can invite users");

        if (!inviterMember.Role.CanInviteMembers)
        {
            throw new UnauthorizedAccessException("Only owners and admins can invite members");
        }

        // Check if user is already a member
        if (org.Members.Any(m => m.UserId == command.InvitedUserId))
        {
            throw new InvalidOperationException("User is already a member of this organization");
        }

        // Check if there's already a pending invitation for this user
        var existingInvitation = await _invitationRepository.GetPendingInvitationAsync(
            command.OrgId,
            command.InvitedUserId,
            cancellationToken);

        if (existingInvitation != null)
        {
            throw new InvalidOperationException("There is already a pending invitation for this user");
        }

        // Verify invited user exists
        var invitedUserExists = await _userRepository.ExistsAsync(command.InvitedUserId, cancellationToken);
        if (!invitedUserExists)
        {
            throw new InvalidOperationException("Invited user not found");
        }

        // Parse and validate role
        var role = OrgRole.Create(command.Role);

        // Create invitation
        var invitation = new Invitation(
            command.OrgId,
            command.InvitedUserId,
            command.InviterUserId,
            role
        );

        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateInvitationResult(
            invitation.Id,
            invitation.OrgId,
            invitation.InvitedUserId,
            invitation.Role.Value,
            invitation.CreatedAt
        );
    }
}
