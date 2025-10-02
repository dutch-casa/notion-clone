using Backend.Application.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Infrastructure.Persistence;

/// <summary>
/// Wraps EF Core's IDbContextTransaction to implement the Application layer's IDbTransaction interface.
/// This maintains Clean Architecture by preventing Application layer from depending on EF Core.
/// </summary>
internal class DbTransactionWrapper : IDbTransaction
{
    private readonly IDbContextTransaction _transaction;

    public DbTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }
}
