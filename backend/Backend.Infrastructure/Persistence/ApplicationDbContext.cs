using Backend.Domain.Aggregates;
using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the application.
/// Repositories encapsulate all data access logic, keeping EF Core concerns in the Infrastructure layer.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
