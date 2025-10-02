namespace Backend.Application.Persistence;

/// <summary>
/// Represents a database transaction abstraction.
/// Allows Application layer to control transaction boundaries without depending on EF Core.
/// </summary>
public interface IDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
