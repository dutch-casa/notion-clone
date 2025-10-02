namespace Backend.Application.UseCases.Organizations.ListInvitations;

public record ListInvitationsCommand(
    Guid UserId
);

public record InvitationDto(
    Guid InvitationId,
    Guid OrgId,
    string OrgName,
    Guid InviterUserId,
    string InviterName,
    string Role,
    DateTimeOffset CreatedAt
);
