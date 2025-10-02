using Backend.Domain.Entities;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.DeclineInvitation;

public class DeclineInvitationHandler
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeclineInvitationHandler(
        IInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork)
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        DeclineInvitationCommand command,
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

        // Decline the invitation
        invitation.Decline();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
