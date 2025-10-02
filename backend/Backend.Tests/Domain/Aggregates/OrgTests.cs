using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;
using DomainOrg = Backend.Domain.Aggregates.Org;

namespace Backend.Tests.Domain.Aggregates;

public class OrgTests
{
    [Fact]
    public void Create_WithValidOwner_ShouldSucceed()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var name = "Acme Corp";

        // Act
        var org = new DomainOrg(name, ownerId);

        // Assert
        org.Id.Should().NotBe(Guid.Empty);
        org.Name.Should().Be(name);
        org.OwnerId.Should().Be(ownerId);
        org.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        org.Members.Should().ContainSingle()
            .Which.Should().Match<Member>(m =>
                m.UserId == ownerId &&
                m.Role == OrgRole.Owner);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        Action act = () => new DomainOrg(name, ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Name*");
    }

    [Fact]
    public void Create_WithEmptyOwnerId_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "Acme Corp";

        // Act
        Action act = () => new DomainOrg(name, Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Owner*");
    }

    [Fact]
    public void AddMember_WithValidData_ShouldSucceed()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var userId = Guid.NewGuid();
        var role = OrgRole.Member;

        // Act
        var member = org.AddMember(userId, role);

        // Assert
        member.Should().NotBeNull();
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(role);
        org.Members.Should().HaveCount(2); // Owner + new member
        org.Members.Should().Contain(member);
    }

    [Fact]
    public void AddMember_WithDuplicateUserId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var org = new DomainOrg("Acme Corp", ownerId);

        // Act
        Action act = () => org.AddMember(ownerId, OrgRole.Admin);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public void RemoveMember_WithRegularMember_ShouldSucceed()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var userId = Guid.NewGuid();
        var member = org.AddMember(userId, OrgRole.Member);

        // Act
        org.RemoveMember(userId);

        // Assert
        org.Members.Should().NotContain(member);
        org.Members.Should().ContainSingle(); // Only owner remains
    }

    [Fact]
    public void RemoveMember_WithOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var org = new DomainOrg("Acme Corp", ownerId);

        // Act
        Action act = () => org.RemoveMember(ownerId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot remove owner*");
    }

    [Fact]
    public void RemoveMember_WithNonExistentMember_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var nonExistentUserId = Guid.NewGuid();

        // Act
        Action act = () => org.RemoveMember(nonExistentUserId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a member*");
    }

    [Fact]
    public void UpdateMemberRole_WithValidData_ShouldSucceed()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var userId = Guid.NewGuid();
        org.AddMember(userId, OrgRole.Member);

        // Act
        org.UpdateMemberRole(userId, OrgRole.Admin);

        // Assert
        var member = org.Members.First(m => m.UserId == userId);
        member.Role.Should().Be(OrgRole.Admin);
    }

    [Fact]
    public void UpdateMemberRole_WithOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var org = new DomainOrg("Acme Corp", ownerId);

        // Act
        Action act = () => org.UpdateMemberRole(ownerId, OrgRole.Admin);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*owner role cannot be changed*");
    }

    [Fact]
    public void GetMember_WithExistingUserId_ShouldReturnMember()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var userId = Guid.NewGuid();
        var addedMember = org.AddMember(userId, OrgRole.Member);

        // Act
        var member = org.GetMember(userId);

        // Assert
        member.Should().Be(addedMember);
    }

    [Fact]
    public void GetMember_WithNonExistentUserId_ShouldReturnNull()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var member = org.GetMember(nonExistentUserId);

        // Assert
        member.Should().BeNull();
    }

    [Fact]
    public void Rename_WithValidName_ShouldSucceed()
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());
        var newName = "New Corp Name";

        // Act
        org.Rename(newName);

        // Assert
        org.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rename_WithEmptyName_ShouldThrowArgumentException(string newName)
    {
        // Arrange
        var org = new DomainOrg("Acme Corp", Guid.NewGuid());

        // Act
        Action act = () => org.Rename(newName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Name*");
    }
}
