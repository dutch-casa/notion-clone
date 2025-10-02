using Backend.Domain.Aggregates;

namespace Backend.Domain.Repositories;

/// <summary>
/// Repository interface for Page aggregate operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IPageRepository
{
    Task<Page?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Page?> GetByIdWithBlocksAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Page>> GetPagesByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task AddAsync(Page page, CancellationToken cancellationToken = default);
    void Update(Page page);
    void Remove(Page page);
}
