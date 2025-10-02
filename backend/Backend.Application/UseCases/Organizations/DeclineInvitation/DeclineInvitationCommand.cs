namespace Backend.Application.UseCases.Organizations.DeclineInvitation;

public record DeclineInvitationCommand(
    Guid InvitationId,
    Guid UserId
);
