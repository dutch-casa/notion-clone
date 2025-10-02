using Backend.Domain.Entities;

namespace Backend.Domain.Repositories;

/// <summary>
/// Repository interface for Block entity operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IBlockRepository
{
    Task<Block?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Block block, CancellationToken cancellationToken = default);
    void Update(Block block);
    void Remove(Block block);
}
