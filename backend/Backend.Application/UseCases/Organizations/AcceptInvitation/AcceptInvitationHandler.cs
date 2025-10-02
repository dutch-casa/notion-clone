using Backend.Domain.Aggregates;
using Backend.Domain.Entities;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.AcceptInvitation;

public class AcceptInvitationHandler
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptInvitationHandler(
        IInvitationRepository invitationRepository,
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _invitationRepository = invitationRepository;
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AcceptInvitationResult> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get invitation
        var invitation = await _invitationRepository.GetByIdAsync(command.InvitationId, cancellationToken)
            ?? throw new InvalidOperationException("Invitation not found");

        // Verify the user is the invited user
        if (invitation.InvitedUserId != command.UserId)
        {
            throw new UnauthorizedAccessException("This invitation is not for you");
        }

        // Verify invitation is still pending
        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Invitation is {invitation.Status.ToString().ToLower()}");
        }

        // Get organization
        var org = await _orgRepository.GetByIdWithMembersAsync(invitation.OrgId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found");

        // Check if user is already a member
        if (org.Members.Any(m => m.UserId == command.UserId))
        {
            throw new InvalidOperationException("User is already a member of this organization");
        }

        // Accept the invitation
        invitation.Accept();

        // Add user as member
        var member = org.AddMember(command.UserId, invitation.Role);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcceptInvitationResult(
            org.Id,
            member.Id,
            member.Role.Value
        );
    }
}
