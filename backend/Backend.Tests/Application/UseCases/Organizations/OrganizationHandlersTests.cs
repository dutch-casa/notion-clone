using Backend.Application.UseCases.Organizations.CreateOrganization;
using Backend.Application.UseCases.Organizations.ListOrganizations;
using Backend.Application.UseCases.Organizations.GetOrganization;
using Backend.Application.UseCases.Organizations.InviteMember;
using Backend.Application.UseCases.Organizations.RemoveMember;
using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Application.UseCases.Organizations;

public class OrganizationHandlersTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public OrganizationHandlersTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateOrganizationHandler_WithValidData_ShouldCreateOrganization()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var user = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(context);
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new CreateOrganizationHandler(userRepository, orgRepository, unitOfWork);
        var command = new CreateOrganizationCommand("Test Org", user.Id);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.OrgId.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("Test Org");
        result.OwnerId.Should().Be(user.Id);

        var org = await context.Orgs.FindAsync(result.OrgId);
        org.Should().NotBeNull();
        org!.Members.Should().ContainSingle()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateOrganizationHandler_WithNonExistentUser_ShouldThrow()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(context);
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new CreateOrganizationHandler(userRepository, orgRepository, unitOfWork);
        var command = new CreateOrganizationCommand("Test Org", Guid.NewGuid());

        // Act
        var act = () => handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task ListOrganizationsHandler_ShouldReturnOnlyUserOrgs()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var user1 = new User(Email.Create($"user1-{guid}@test.com"), "User 1", "hash");
        var user2 = new User(Email.Create($"user2-{guid}@test.com"), "User 2", "hash");
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var org1 = new Backend.Domain.Aggregates.Org("User 1 Org", user1.Id);
        var org2 = new Backend.Domain.Aggregates.Org("User 2 Org", user2.Id);
        var org3 = new Backend.Domain.Aggregates.Org("Shared Org", user1.Id);
        org3.AddMember(user2.Id, OrgRole.Member);

        context.Orgs.AddRange(org1, org2, org3);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var handler = new ListOrganizationsHandler(orgRepository);
        var command = new ListOrganizationsCommand(user2.Id);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        var orgList = result.ToList();
        orgList.Should().HaveCount(2);
        orgList.Should().Contain(o => o.Name == "User 2 Org");
        orgList.Should().Contain(o => o.Name == "Shared Org");
        orgList.Should().NotContain(o => o.Name == "User 1 Org");
    }

    [Fact]
    public async Task GetOrganizationHandler_WithValidMember_ShouldReturnOrgDetails()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var owner = new User(Email.Create($"owner-{guid}@test.com"), "Owner", "hash");
        var member = new User(Email.Create($"member-{guid}@test.com"), "Member", "hash");
        context.Users.AddRange(owner, member);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        org.AddMember(member.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(context);
        var handler = new GetOrganizationHandler(orgRepository, userRepository);
        var command = new GetOrganizationCommand(org.Id, member.Id);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Id.Should().Be(org.Id);
        result.Name.Should().Be("Test Org");
        result.OwnerId.Should().Be(owner.Id);
        result.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrganizationHandler_WithNonMember_ShouldThrowUnauthorized()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var owner = new User(Email.Create($"owner-{guid}@test.com"), "Owner", "hash");
        var outsider = new User(Email.Create($"outsider-{guid}@test.com"), "Outsider", "hash");
        context.Users.AddRange(owner, outsider);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(context);
        var handler = new GetOrganizationHandler(orgRepository, userRepository);
        var command = new GetOrganizationCommand(org.Id, outsider.Id);

        // Act
        var act = () => handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not a member*");
    }

    [Fact]
    public async Task InviteMemberHandler_ByOwner_ShouldAddMember()
    {
        // Arrange
        using var setupContext = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var owner = new User(Email.Create($"owner-{guid}@test.com"), "Owner", "hash");
        var newMember = new User(Email.Create($"member-{guid}@test.com"), "Member", "hash");
        setupContext.Users.AddRange(owner, newMember);
        await setupContext.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        setupContext.Orgs.Add(org);
        await setupContext.SaveChangesAsync();
        var orgId = org.Id;

        // Act - use fresh context for handler
        using var handlerContext = _fixture.CreateNewContext();
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(handlerContext);
        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(handlerContext);
        var unitOfWork = _fixture.CreateUnitOfWork(handlerContext);
        var handler = new InviteMemberHandler(orgRepository, userRepository, unitOfWork);
        var command = new InviteMemberCommand(orgId, newMember.Id, "Member", owner.Id);
        var result = await handler.HandleAsync(command);

        // Assert
        result.UserId.Should().Be(newMember.Id);
        result.Role.Should().Be("member");

        using var verifyContext = _fixture.CreateNewContext();
        var updatedOrg = await verifyContext.Orgs
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == orgId);
        updatedOrg!.Members.Should().HaveCount(2);
        updatedOrg.Members.Should().Contain(m => m.UserId == newMember.Id);
    }

    [Fact]
    public async Task InviteMemberHandler_ByMember_ShouldThrowUnauthorized()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var owner = new User(Email.Create($"owner-{guid}@test.com"), "Owner", "hash");
        var regularMember = new User(Email.Create($"member-{guid}@test.com"), "Member", "hash");
        var newMember = new User(Email.Create($"newmember-{guid}@test.com"), "New Member", "hash");
        context.Users.AddRange(owner, regularMember, newMember);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        org.AddMember(regularMember.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new InviteMemberHandler(orgRepository, userRepository, unitOfWork);
        var command = new InviteMemberCommand(org.Id, newMember.Id, "Member", regularMember.Id);

        // Act
        var act = () => handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*owners and admins*");
    }

    [Fact]
    public async Task RemoveMemberHandler_ByOwner_ShouldRemoveMember()
    {
        // Arrange
        using var setupContext = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var owner = new User(Email.Create($"owner-{guid}@test.com"), "Owner", "hash");
        var member = new User(Email.Create($"member-{guid}@test.com"), "Member", "hash");
        setupContext.Users.AddRange(owner, member);
        await setupContext.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        org.AddMember(member.Id, OrgRole.Member);
        setupContext.Orgs.Add(org);
        await setupContext.SaveChangesAsync();
        var orgId = org.Id;

        // Act - use fresh context for handler
        using var handlerContext = _fixture.CreateNewContext();
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(handlerContext);
        var unitOfWork = _fixture.CreateUnitOfWork(handlerContext);
        var handler = new RemoveMemberHandler(orgRepository, unitOfWork);
        var command = new RemoveMemberCommand(orgId, member.Id, owner.Id);
        await handler.HandleAsync(command);

        // Assert
        using var verifyContext = _fixture.CreateNewContext();
        var updatedOrg = await verifyContext.Orgs
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == orgId);
        updatedOrg!.Members.Should().ContainSingle()
            .Which.UserId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task RemoveMemberHandler_RemovingOwner_ShouldThrow()
    {
        // Arrange
        using var setupContext = _fixture.CreateNewContext();

        var guid = Guid.NewGuid();
        var owner = new User(Email.Create($"owner-{guid}@test.com"), "Owner", "hash");
        setupContext.Users.Add(owner);
        await setupContext.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        setupContext.Orgs.Add(org);
        await setupContext.SaveChangesAsync();
        var orgId = org.Id;

        // Act - use fresh context for handler
        using var handlerContext = _fixture.CreateNewContext();
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(handlerContext);
        var unitOfWork = _fixture.CreateUnitOfWork(handlerContext);
        var handler = new RemoveMemberHandler(orgRepository, unitOfWork);
        var command = new RemoveMemberCommand(orgId, owner.Id, owner.Id);
        var act = () => handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot remove owner*");
    }
}
