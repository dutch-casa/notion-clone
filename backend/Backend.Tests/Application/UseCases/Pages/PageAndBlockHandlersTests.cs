using Backend.Application.UseCases.Pages.CreatePage;
using Backend.Application.UseCases.Pages.GetPage;
using Backend.Application.UseCases.Blocks.AddBlock;
using Backend.Application.UseCases.Blocks.UpdateBlock;
using Backend.Application.UseCases.Blocks.RemoveBlock;
using Backend.Application.UseCases.Organizations.CreateOrganization;
using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Application.UseCases.Pages;

public class PageAndBlockHandlersTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public PageAndBlockHandlersTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreatePage_AndAddBlocks_ShouldPersistCorrectly()
    {
        // Arrange - Create user and org
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"test-{Guid.NewGuid()}@example.com"), "Test User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var createOrgCommand = new CreateOrganizationCommand("Test Org", user.Id);
        var orgResult = await createOrgHandler.HandleAsync(createOrgCommand);

        // Act - Create page
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var createPageCommand = new CreatePageCommand(orgResult.OrgId, "My Document", user.Id);
        var pageResult = await createPageHandler.HandleAsync(createPageCommand);

        // Assert - Page created
        pageResult.Should().NotBeNull();
        pageResult.Title.Should().Be("My Document");

        // Act - Add first block (paragraph)
        var addBlockHandler = new AddBlockHandler(pageRepository, orgRepository, unitOfWork);
        var addBlock1Command = new AddBlockCommand(
            pageResult.Id,
            1.0m, // sortKey
            "paragraph",
            null, // no parent
            "{\"text\":\"First paragraph\"}",
            user.Id);
        var block1Result = await addBlockHandler.HandleAsync(addBlock1Command);

        // Assert - First block created
        block1Result.Should().NotBeNull();
        block1Result.Type.Should().Be("paragraph");
        block1Result.SortKey.Should().Be(1.0m);
        block1Result.Json.Should().Be("{\"text\":\"First paragraph\"}");

        // Act - Add second block (heading)
        var addBlock2Command = new AddBlockCommand(
            pageResult.Id,
            2.0m,
            "heading",
            null,
            "{\"text\":\"Section Title\",\"level\":2}",
            user.Id);
        var block2Result = await addBlockHandler.HandleAsync(addBlock2Command);

        // Act - Get page with blocks
        var getPageHandler = new GetPageHandler(pageRepository, orgRepository);
        var getPageQuery = new GetPageQuery(pageResult.Id, user.Id);
        var fullPage = await getPageHandler.HandleAsync(getPageQuery);

        // Assert - Page contains both blocks in correct order
        fullPage.Should().NotBeNull();
        fullPage.Blocks.Should().HaveCount(2);
        fullPage.Blocks.Should().BeInAscendingOrder(b => b.SortKey);
        fullPage.Blocks.First().Type.Should().Be("paragraph");
        fullPage.Blocks.Last().Type.Should().Be("heading");
    }

    [Fact]
    public async Task UpdateBlock_ShouldModifyBlockContent()
    {
        // Arrange - Create user, org, page, and block
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"update-{Guid.NewGuid()}@example.com"), "Update User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var orgResult = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Update Org", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(orgResult.OrgId, "Update Test", user.Id));

        var blockRepository = new Backend.Infrastructure.Persistence.Repositories.BlockRepository(dbContext);
        var addBlockHandler = new AddBlockHandler(pageRepository, orgRepository, unitOfWork);
        var blockResult = await addBlockHandler.HandleAsync(
            new AddBlockCommand(
                pageResult.Id,
                1.0m,
                "paragraph",
                null,
                "{\"text\":\"Original content\"}",
                user.Id));

        // Act - Update block
        var updateBlockHandler = new UpdateBlockHandler(blockRepository, pageRepository, orgRepository, unitOfWork);
        await updateBlockHandler.HandleAsync(
            new UpdateBlockCommand(
                blockResult.Id,
                "paragraph",
                1.0m,
                "{\"text\":\"Updated content\"}",
                user.Id));

        // Assert - Block was updated
        var getPageHandler = new GetPageHandler(pageRepository, orgRepository);
        var fullPage = await getPageHandler.HandleAsync(
            new GetPageQuery(pageResult.Id, user.Id));

        fullPage.Blocks.Should().ContainSingle();
        fullPage.Blocks.First().Json.Should().Be("{\"text\":\"Updated content\"}");
    }

    [Fact]
    public async Task RemoveBlock_ShouldDeleteBlockFromPage()
    {
        // Arrange - Create user, org, page, and blocks
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"remove-{Guid.NewGuid()}@example.com"), "Remove User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var orgResult = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Remove Org", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(orgResult.OrgId, "Remove Test", user.Id));

        var blockRepository = new Backend.Infrastructure.Persistence.Repositories.BlockRepository(dbContext);
        var addBlockHandler = new AddBlockHandler(pageRepository, orgRepository, unitOfWork);
        var block1Result = await addBlockHandler.HandleAsync(
            new AddBlockCommand(pageResult.Id, 1.0m, "paragraph", null, "{\"text\":\"Block 1\"}", user.Id));

        var block2Result = await addBlockHandler.HandleAsync(
            new AddBlockCommand(pageResult.Id, 2.0m, "paragraph", null, "{\"text\":\"Block 2\"}", user.Id));

        // Act - Remove first block
        var removeBlockHandler = new RemoveBlockHandler(blockRepository, pageRepository, orgRepository, unitOfWork);
        await removeBlockHandler.HandleAsync(
            new RemoveBlockCommand(block1Result.Id, user.Id));

        // Assert - Only second block remains
        var getPageHandler = new GetPageHandler(pageRepository, orgRepository);
        var fullPage = await getPageHandler.HandleAsync(
            new GetPageQuery(pageResult.Id, user.Id));

        fullPage.Blocks.Should().ContainSingle();
        fullPage.Blocks.First().Id.Should().Be(block2Result.Id);
        fullPage.Blocks.First().Json.Should().Be("{\"text\":\"Block 2\"}");
    }

    [Fact]
    public async Task AddBlock_WithFractionalSortKey_ShouldMaintainCorrectOrder()
    {
        // Arrange
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"sort-{Guid.NewGuid()}@example.com"), "Sort User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var orgResult = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Sort Org", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(orgResult.OrgId, "Sort Test", user.Id));

        var addBlockHandler = new AddBlockHandler(pageRepository, orgRepository, unitOfWork);

        // Add blocks in non-sequential order
        await addBlockHandler.HandleAsync(
            new AddBlockCommand(pageResult.Id, 1.0m, "paragraph", null, "{\"text\":\"First\"}", user.Id));

        await addBlockHandler.HandleAsync(
            new AddBlockCommand(pageResult.Id, 3.0m, "paragraph", null, "{\"text\":\"Third\"}", user.Id));

        // Insert between first and third using fractional sort key
        await addBlockHandler.HandleAsync(
            new AddBlockCommand(pageResult.Id, 2.0m, "paragraph", null, "{\"text\":\"Second\"}", user.Id));

        // Assert - Blocks are in correct order
        var getPageHandler = new GetPageHandler(pageRepository, orgRepository);
        var fullPage = await getPageHandler.HandleAsync(
            new GetPageQuery(pageResult.Id, user.Id));

        fullPage.Blocks.Should().HaveCount(3);
        fullPage.Blocks.Should().BeInAscendingOrder(b => b.SortKey);
        fullPage.Blocks.Select(b => b.Json).Should().ContainInOrder(
            "{\"text\":\"First\"}",
            "{\"text\":\"Second\"}",
            "{\"text\":\"Third\"}");
    }

    [Fact]
    public async Task CreatePage_WithMultipleBlocks_ShouldPersistAllBlocks()
    {
        // Arrange
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"multi-{Guid.NewGuid()}@example.com"), "Multi User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var orgResult = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Multi Org", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(orgResult.OrgId, "Rich Document", user.Id));

        var addBlockHandler = new AddBlockHandler(pageRepository, orgRepository, unitOfWork);

        // Act - Create a rich document with various block types
        var blocks = new[]
        {
            new { sortKey = 1.0m, type = "heading", json = "{\"text\":\"Document Title\",\"level\":1}" },
            new { sortKey = 2.0m, type = "paragraph", json = "{\"text\":\"Introduction paragraph\"}" },
            new { sortKey = 3.0m, type = "heading", json = "{\"text\":\"Section 1\",\"level\":2}" },
            new { sortKey = 4.0m, type = "paragraph", json = "{\"text\":\"Section content\"}" },
            new { sortKey = 5.0m, type = "todo", json = "{\"text\":\"Task to complete\",\"checked\":false}" },
            new { sortKey = 6.0m, type = "paragraph", json = "{\"text\":\"Conclusion\"}" },
        };

        foreach (var block in blocks)
        {
            await addBlockHandler.HandleAsync(
                new AddBlockCommand(pageResult.Id, block.sortKey, block.type, null, block.json, user.Id));
        }

        // Assert - All blocks persisted correctly
        var getPageHandler = new GetPageHandler(pageRepository, orgRepository);
        var fullPage = await getPageHandler.HandleAsync(
            new GetPageQuery(pageResult.Id, user.Id));

        fullPage.Blocks.Should().HaveCount(6);
        fullPage.Blocks.Should().BeInAscendingOrder(b => b.SortKey);
        fullPage.Blocks.Select(b => b.Type).Should().ContainInOrder(
            "heading", "paragraph", "heading", "paragraph", "todo", "paragraph");
        fullPage.Blocks.First().Json.Should().Contain("Document Title");
        fullPage.Blocks.Last().Json.Should().Contain("Conclusion");
    }
}
