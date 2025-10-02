namespace Backend.Application.UseCases.Organizations.CreateInvitation;

public record CreateInvitationCommand(
    Guid OrgId,
    Guid InvitedUserId,
    string Role,
    Guid InviterUserId
);

public record CreateInvitationByEmailCommand(
    Guid OrgId,
    string InvitedUserEmail,
    string Role,
    Guid InviterUserId
);

public record CreateInvitationResult(
    Guid InvitationId,
    Guid OrgId,
    Guid InvitedUserId,
    string Role,
    DateTimeOffset CreatedAt
);
