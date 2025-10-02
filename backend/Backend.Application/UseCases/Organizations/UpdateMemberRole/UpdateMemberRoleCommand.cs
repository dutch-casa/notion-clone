namespace Backend.Application.UseCases.Organizations.UpdateMemberRole;

public record UpdateMemberRoleCommand(
    Guid OrgId,
    Guid MemberId,
    string NewRole,
    Guid RequestingUserId
);

public record UpdateMemberRoleResult(
    Guid MemberId,
    Guid UserId,
    string Role,
    DateTimeOffset JoinedAt
);
