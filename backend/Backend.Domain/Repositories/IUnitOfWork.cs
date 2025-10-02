namespace Backend.Domain.Repositories;

/// <summary>
/// Unit of Work pattern interface for coordinating repository operations.
/// Owned by Domain layer to enforce dependency inversion.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
