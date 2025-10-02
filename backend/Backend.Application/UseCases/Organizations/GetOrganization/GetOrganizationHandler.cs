using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.GetOrganization;

public class GetOrganizationHandler
{
    private readonly IOrgRepository _orgRepository;
    private readonly IUserRepository _userRepository;

    public GetOrganizationHandler(
        IOrgRepository orgRepository,
        IUserRepository userRepository)
    {
        _orgRepository = orgRepository;
        _userRepository = userRepository;
    }

    public async Task<GetOrganizationResult> HandleAsync(
        GetOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var org = await _orgRepository.GetByIdWithMembersAsync(command.OrgId, cancellationToken);

        if (org == null)
        {
            throw new InvalidOperationException($"Organization {command.OrgId} not found");
        }

        // Verify requesting user is a member
        if (!org.Members.Any(m => m.UserId == command.RequestingUserId))
        {
            throw new UnauthorizedAccessException("You are not a member of this organization");
        }

        // Get member user IDs and fetch user info
        var memberUserIds = org.Members.Select(m => m.UserId).ToList();
        var users = await _userRepository.GetByIdsAsync(memberUserIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id);

        var members = org.Members.Select(m => new MemberDto(
            m.UserId,
            userDict.TryGetValue(m.UserId, out var user) ? user.Name : "Unknown User",
            userDict.TryGetValue(m.UserId, out var u) ? u.Email.Value : "",
            m.Role.Value,
            m.JoinedAt)).ToList();

        return new GetOrganizationResult(
            org.Id,
            org.Name,
            org.OwnerId,
            org.CreatedAt,
            members);
    }
}
