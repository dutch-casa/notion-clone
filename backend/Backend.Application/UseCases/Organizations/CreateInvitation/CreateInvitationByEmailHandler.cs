using Backend.Application.Services;
using Backend.Domain.Entities;
using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Organizations.CreateInvitation;

public class CreateInvitationByEmailHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IInvitationNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInvitationByEmailHandler(
        IUserRepository userRepository,
        IOrgRepository orgRepository,
        IInvitationRepository invitationRepository,
        IInvitationNotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _orgRepository = orgRepository;
        _invitationRepository = invitationRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateInvitationResult> HandleAsync(
        CreateInvitationByEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        // Lookup user by email
        var invitedUser = await _userRepository.GetByEmailAsync(
            Email.Create(command.InvitedUserEmail),
            cancellationToken);

        if (invitedUser == null)
        {
            throw new InvalidOperationException($"No user found with email address: {command.InvitedUserEmail}");
        }

        // Verify organization exists and load members
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
        if (org.Members.Any(m => m.UserId == invitedUser.Id))
        {
            throw new InvalidOperationException("User is already a member of this organization");
        }

        // Check if there's already a pending invitation for this user
        var existingInvitation = await _invitationRepository.GetPendingInvitationAsync(
            command.OrgId,
            invitedUser.Id,
            cancellationToken);

        if (existingInvitation != null)
        {
            throw new InvalidOperationException("There is already a pending invitation for this user");
        }

        // Parse and validate role
        var role = OrgRole.Create(command.Role);

        // Create invitation
        var invitation = new Invitation(
            command.OrgId,
            invitedUser.Id,
            command.InviterUserId,
            role
        );

        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish notification event
        var inviter = await _userRepository.GetByIdAsync(command.InviterUserId, cancellationToken);
        var notification = new InvitationNotification(
            invitation.Id,
            invitation.OrgId,
            org.Name,
            invitation.InviterUserId,
            inviter?.Name ?? "Unknown",
            invitation.Role.Value,
            invitation.CreatedAt
        );

        await _notificationService.PublishInvitationCreatedAsync(
            invitation.InvitedUserId,
            notification,
            cancellationToken
        );

        return new CreateInvitationResult(
            invitation.Id,
            invitation.OrgId,
            invitation.InvitedUserId,
            invitation.Role.Value,
            invitation.CreatedAt
        );
    }
}
