using Backend.Application.UseCases.Organizations.UpdateMemberRole;
using Backend.Domain.Aggregates;
using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Application.UseCases.Organizations.UpdateMemberRole;

public class UpdateMemberRoleHandlerTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public UpdateMemberRoleHandlerTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdateMemberRole_OwnerUpdatingMember_ShouldSucceed()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var owner = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        var targetUser = new User(Email.Create($"member-{Guid.NewGuid()}@test.com"), "Member", "hash");
        context.Users.AddRange(owner, targetUser);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        var targetMember = org.AddMember(targetUser.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var command = new UpdateMemberRoleCommand(
            org.Id,
            targetMember.Id,
            "admin",
            owner.Id
        );

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.MemberId.Should().Be(targetMember.Id);
        result.Role.Should().Be("admin");
    }

    [Fact]
    public async Task UpdateMemberRole_AdminUpdatingMember_ShouldSucceed()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var owner = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        var admin = new User(Email.Create($"admin-{Guid.NewGuid()}@test.com"), "Admin", "hash");
        var targetUser = new User(Email.Create($"member-{Guid.NewGuid()}@test.com"), "Member", "hash");
        context.Users.AddRange(owner, admin, targetUser);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        var adminMember = org.AddMember(admin.Id, OrgRole.Admin);
        var targetMember = org.AddMember(targetUser.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var command = new UpdateMemberRoleCommand(
            org.Id,
            targetMember.Id,
            "admin",
            admin.Id
        );

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be("admin");
    }

    [Fact]
    public async Task UpdateMemberRole_RegularMemberUpdating_ShouldThrowUnauthorized()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var owner = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        var member1 = new User(Email.Create($"member1-{Guid.NewGuid()}@test.com"), "Member1", "hash");
        var member2 = new User(Email.Create($"member2-{Guid.NewGuid()}@test.com"), "Member2", "hash");
        context.Users.AddRange(owner, member1, member2);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        var member1Entity = org.AddMember(member1.Id, OrgRole.Member);
        var member2Entity = org.AddMember(member2.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var command = new UpdateMemberRoleCommand(
            org.Id,
            member2Entity.Id,
            "admin",
            member1.Id
        );

        // Act & Assert
        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only owners and admins can update member roles");
    }

    [Fact]
    public async Task UpdateMemberRole_MemberNotFound_ShouldThrowException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var owner = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var nonExistentMemberId = Guid.NewGuid();
        var command = new UpdateMemberRoleCommand(
            org.Id,
            nonExistentMemberId,
            "admin",
            owner.Id
        );

        // Act & Assert
        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Member not found");
    }

    [Fact]
    public async Task UpdateMemberRole_OrganizationNotFound_ShouldThrowException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var nonExistentOrgId = Guid.NewGuid();
        var command = new UpdateMemberRoleCommand(
            nonExistentOrgId,
            Guid.NewGuid(),
            "admin",
            Guid.NewGuid()
        );

        // Act & Assert
        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Organization not found");
    }

    [Fact]
    public async Task UpdateMemberRole_InvalidRole_ShouldThrowException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var owner = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        var targetUser = new User(Email.Create($"member-{Guid.NewGuid()}@test.com"), "Member", "hash");
        context.Users.AddRange(owner, targetUser);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        var targetMember = org.AddMember(targetUser.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var command = new UpdateMemberRoleCommand(
            org.Id,
            targetMember.Id,
            "invalid_role",
            owner.Id
        );

        // Act & Assert
        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateMemberRole_RequestingUserNotMember_ShouldThrowUnauthorized()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var owner = new User(Email.Create($"owner-{Guid.NewGuid()}@test.com"), "Owner", "hash");
        var targetUser = new User(Email.Create($"member-{Guid.NewGuid()}@test.com"), "Member", "hash");
        var outsider = new User(Email.Create($"outsider-{Guid.NewGuid()}@test.com"), "Outsider", "hash");
        context.Users.AddRange(owner, targetUser, outsider);
        await context.SaveChangesAsync();

        var org = new Backend.Domain.Aggregates.Org("Test Org", owner.Id);
        var targetMember = org.AddMember(targetUser.Id, OrgRole.Member);
        context.Orgs.Add(org);
        await context.SaveChangesAsync();

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(context);
        var unitOfWork = _fixture.CreateUnitOfWork(context);
        var handler = new UpdateMemberRoleHandler(orgRepository, unitOfWork);
        var command = new UpdateMemberRoleCommand(
            org.Id,
            targetMember.Id,
            "admin",
            outsider.Id
        );

        // Act & Assert
        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only owners and admins can update member roles");
    }
}
