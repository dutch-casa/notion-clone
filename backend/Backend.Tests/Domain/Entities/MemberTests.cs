using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.Entities;

public class MemberTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = OrgRole.Member;

        // Act
        var member = new Member(orgId, userId, role);

        // Assert
        member.Id.Should().NotBe(Guid.Empty);
        member.OrgId.Should().Be(orgId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(role);
        member.JoinedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyOrgId_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var role = OrgRole.Member;

        // Act
        Action act = () => new Member(Guid.Empty, userId, role);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*OrgId*");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var role = OrgRole.Member;

        // Act
        Action act = () => new Member(orgId, Guid.Empty, role);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*UserId*");
    }

    [Fact]
    public void UpdateRole_WithValidRole_ShouldSucceed()
    {
        // Arrange
        var member = new Member(Guid.NewGuid(), Guid.NewGuid(), OrgRole.Member);
        var newRole = OrgRole.Admin;

        // Act
        member.UpdateRole(newRole);

        // Assert
        member.Role.Should().Be(newRole);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("member")]
    public void Create_WithAllValidRoles_ShouldSucceed(string roleValue)
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = OrgRole.Create(roleValue);

        // Act
        var member = new Member(orgId, userId, role);

        // Assert
        member.Role.Should().Be(role);
    }
}
