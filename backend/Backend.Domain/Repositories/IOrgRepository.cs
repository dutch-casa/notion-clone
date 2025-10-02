using Backend.Domain.Aggregates;

namespace Backend.Domain.Repositories;

/// <summary>
/// Repository interface for Org aggregate operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IOrgRepository
{
    Task<Org?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Org?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Org>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<Org>> GetOrganizationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Org org, CancellationToken cancellationToken = default);
    void Update(Org org);
    void Remove(Org org);
}
