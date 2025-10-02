namespace Backend.Application.UseCases.Organizations.AcceptInvitation;

public record AcceptInvitationCommand(
    Guid InvitationId,
    Guid UserId
);

public record AcceptInvitationResult(
    Guid OrgId,
    Guid MemberId,
    string Role
);
