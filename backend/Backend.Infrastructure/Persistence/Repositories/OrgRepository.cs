using Backend.Domain.Aggregates;
using Backend.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class OrgRepository : IOrgRepository
{
    private readonly ApplicationDbContext _context;

    public OrgRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Org?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orgs
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Org?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orgs
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<List<Org>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Orgs
            .Where(o => ids.Contains(o.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Org>> GetOrganizationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orgs
            .Include(o => o.Members)
            .Where(o => o.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Org org, CancellationToken cancellationToken = default)
    {
        await _context.Orgs.AddAsync(org, cancellationToken);
    }

    public void Update(Org org)
    {
        _context.Orgs.Update(org);
    }

    public void Remove(Org org)
    {
        _context.Orgs.Remove(org);
    }
}
