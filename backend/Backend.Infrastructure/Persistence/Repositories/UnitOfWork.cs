using Backend.Application.Services;
using Backend.Domain.Common;
using Backend.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UnitOfWork(
        ApplicationDbContext context,
        IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var aggregatesWithEvents = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        // Save changes first
        var result = await _context.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        foreach (var aggregate in aggregatesWithEvents)
        {
            var events = aggregate.DomainEvents;
            foreach (var domainEvent in events)
            {
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}
