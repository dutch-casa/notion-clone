using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;

namespace Backend.Domain.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<List<User>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    void Remove(User user);
}
