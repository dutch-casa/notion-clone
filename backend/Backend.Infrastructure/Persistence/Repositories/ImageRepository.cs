using Backend.Domain.Aggregates;
using Backend.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly ApplicationDbContext _context;

    public ImageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Images
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task AddAsync(Image image, CancellationToken cancellationToken = default)
    {
        await _context.Images.AddAsync(image, cancellationToken);
    }

    public void Remove(Image image)
    {
        _context.Images.Remove(image);
    }
}
