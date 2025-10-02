using Backend.Domain.Entities;
using Backend.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly ApplicationDbContext _context;

    public BlockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Block?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AddAsync(Block block, CancellationToken cancellationToken = default)
    {
        await _context.Blocks.AddAsync(block, cancellationToken);
    }

    public void Update(Block block)
    {
        _context.Blocks.Update(block);
    }

    public void Remove(Block block)
    {
        _context.Blocks.Remove(block);
    }
}
