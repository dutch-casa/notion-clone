using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Persistence.Repositories;
using Backend.Tests.Infrastructure.Stubs;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Backend.Tests.Application;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public ApplicationDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        DbContext = new ApplicationDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Creates a UnitOfWork with stub domain event dispatcher for testing.
    /// Following Clean Architecture: tests use stubs to avoid infrastructure dependencies.
    /// </summary>
    public UnitOfWork CreateUnitOfWork(ApplicationDbContext context)
    {
        return new UnitOfWork(
            context,
            new StubDomainEventDispatcher());
    }
}
