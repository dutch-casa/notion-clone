using Backend.Domain.Entities;
using Backend.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly ApplicationDbContext _context;

    public InvitationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<List<Invitation>> GetByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .Where(i => i.OrgId == orgId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invitation>> GetByInvitedUserIdAsync(Guid invitedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .Where(i => i.InvitedUserId == invitedUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invitation>> GetPendingByInvitedUserIdAsync(Guid invitedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .Where(i => i.InvitedUserId == invitedUserId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invitation?> GetPendingInvitationAsync(Guid orgId, Guid invitedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .FirstOrDefaultAsync(i =>
                i.OrgId == orgId &&
                i.InvitedUserId == invitedUserId &&
                i.Status == InvitationStatus.Pending,
                cancellationToken);
    }

    public async Task<bool> ExistsForOrgAndUserAsync(Guid orgId, Guid invitedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .AnyAsync(i => i.OrgId == orgId && i.InvitedUserId == invitedUserId, cancellationToken);
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        await _context.Invitations.AddAsync(invitation, cancellationToken);
    }

    public void Update(Invitation invitation)
    {
        _context.Invitations.Update(invitation);
    }

    public void Remove(Invitation invitation)
    {
        _context.Invitations.Remove(invitation);
    }
}
