namespace Backend.Application.UseCases.Organizations.InviteMember;

public record InviteMemberCommand(Guid OrgId, Guid UserId, string Role, Guid RequestingUserId);

public record InviteMemberResult(Guid MemberId, Guid UserId, string Role, DateTimeOffset JoinedAt);
