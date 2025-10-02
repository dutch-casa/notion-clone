using Backend.Domain.Aggregates;
using Backend.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class PageRepository : IPageRepository
{
    private readonly ApplicationDbContext _context;

    public PageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Page?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Page?> GetByIdWithBlocksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .Include(p => p.Blocks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Page>> GetPagesByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .Where(p => p.OrgId == orgId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Page page, CancellationToken cancellationToken = default)
    {
        await _context.Pages.AddAsync(page, cancellationToken);
    }

    public void Update(Page page)
    {
        _context.Pages.Update(page);
    }

    public void Remove(Page page)
    {
        _context.Pages.Remove(page);
    }
}
