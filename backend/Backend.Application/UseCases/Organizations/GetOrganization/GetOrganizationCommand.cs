namespace Backend.Application.UseCases.Organizations.GetOrganization;

public record GetOrganizationCommand(Guid OrgId, Guid RequestingUserId);

public record GetOrganizationResult(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTimeOffset CreatedAt,
    IEnumerable<MemberDto> Members);

public record MemberDto(
    Guid UserId,
    string UserName,
    string UserEmail,
    string Role,
    DateTimeOffset JoinedAt);
