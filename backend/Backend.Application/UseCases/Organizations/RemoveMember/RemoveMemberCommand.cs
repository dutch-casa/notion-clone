namespace Backend.Application.UseCases.Organizations.RemoveMember;

public record RemoveMemberCommand(Guid OrgId, Guid UserId, Guid RequestingUserId);
