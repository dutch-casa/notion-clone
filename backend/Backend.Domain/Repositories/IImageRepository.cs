using Backend.Domain.Aggregates;

namespace Backend.Domain.Repositories;

/// <summary>
/// Repository interface for Image entity operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IImageRepository
{
    Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Image image, CancellationToken cancellationToken = default);
    void Remove(Image image);
}
