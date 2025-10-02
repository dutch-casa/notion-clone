using Backend.Domain.Entities;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.ListInvitations;

public class ListInvitationsHandler
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUserRepository _userRepository;

    public ListInvitationsHandler(
        IInvitationRepository invitationRepository,
        IOrgRepository orgRepository,
        IUserRepository userRepository)
    {
        _invitationRepository = invitationRepository;
        _orgRepository = orgRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<InvitationDto>> HandleAsync(
        ListInvitationsCommand command,
        CancellationToken cancellationToken = default)
    {
        var invitations = await _invitationRepository.GetPendingByInvitedUserIdAsync(
            command.UserId,
            cancellationToken);

        var result = new List<InvitationDto>();

        // Batch fetch organizations and users to avoid N+1 queries
        var orgIds = invitations.Select(i => i.OrgId).Distinct().ToList();
        var userIds = invitations.Select(i => i.InviterUserId).Distinct().ToList();

        var orgs = await _orgRepository.GetByIdsAsync(orgIds, cancellationToken);
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);

        var orgDict = orgs.ToDictionary(o => o.Id);
        var userDict = users.ToDictionary(u => u.Id);

        foreach (var invitation in invitations)
        {
            if (orgDict.TryGetValue(invitation.OrgId, out var org) &&
                userDict.TryGetValue(invitation.InviterUserId, out var inviter))
            {
                result.Add(new InvitationDto(
                    invitation.Id,
                    invitation.OrgId,
                    org.Name,
                    invitation.InviterUserId,
                    inviter.Name,
                    invitation.Role.Value,
                    invitation.CreatedAt
                ));
            }
        }

        return result;
    }
}
