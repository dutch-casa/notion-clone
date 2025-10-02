using Backend.Domain.Entities;

namespace Backend.Domain.Repositories;

/// <summary>
/// Repository interface for Invitation entity operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Invitation>> GetByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task<List<Invitation>> GetByInvitedUserIdAsync(Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<List<Invitation>> GetPendingByInvitedUserIdAsync(Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<Invitation?> GetPendingInvitationAsync(Guid orgId, Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForOrgAndUserAsync(Guid orgId, Guid invitedUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default);
    void Update(Invitation invitation);
    void Remove(Invitation invitation);
}
