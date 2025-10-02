using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class OrgRoleTests
{
    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("member")]
    public void Create_WithValidRole_ShouldSucceed(string roleName)
    {
        // Act
        var result = OrgRole.Create(roleName);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(roleName.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyRole_ShouldThrowArgumentException(string roleName)
    {
        // Act
        Action act = () => OrgRole.Create(roleName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("superadmin")]
    [InlineData("guest")]
    public void Create_WithInvalidRole_ShouldThrowArgumentException(string roleName)
    {
        // Act
        Action act = () => OrgRole.Create(roleName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid role*");
    }

    [Fact]
    public void Create_ShouldNormalizeToLowercase()
    {
        // Arrange & Act
        var result = OrgRole.Create("OWNER");

        // Assert
        result.Value.Should().Be("owner");
    }

    [Fact]
    public void Owner_Property_ShouldReturnOwnerRole()
    {
        // Act
        var owner = OrgRole.Owner;

        // Assert
        owner.Value.Should().Be("owner");
    }

    [Fact]
    public void Admin_Property_ShouldReturnAdminRole()
    {
        // Act
        var admin = OrgRole.Admin;

        // Assert
        admin.Value.Should().Be("admin");
    }

    [Fact]
    public void Member_Property_ShouldReturnMemberRole()
    {
        // Act
        var member = OrgRole.Member;

        // Assert
        member.Value.Should().Be("member");
    }

    [Fact]
    public void IsOwner_WithOwnerRole_ShouldReturnTrue()
    {
        // Arrange
        var role = OrgRole.Owner;

        // Act & Assert
        role.IsOwner.Should().BeTrue();
        role.IsAdmin.Should().BeFalse();
        role.IsMember.Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_WithAdminRole_ShouldReturnTrue()
    {
        // Arrange
        var role = OrgRole.Admin;

        // Act & Assert
        role.IsAdmin.Should().BeTrue();
        role.IsOwner.Should().BeFalse();
        role.IsMember.Should().BeFalse();
    }

    [Fact]
    public void CanInviteMembers_WithOwnerOrAdmin_ShouldReturnTrue()
    {
        // Arrange
        var owner = OrgRole.Owner;
        var admin = OrgRole.Admin;
        var member = OrgRole.Member;

        // Act & Assert
        owner.CanInviteMembers.Should().BeTrue();
        admin.CanInviteMembers.Should().BeTrue();
        member.CanInviteMembers.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var role1 = OrgRole.Owner;
        var role2 = OrgRole.Create("owner");

        // Act & Assert
        role1.Should().Be(role2);
        (role1 == role2).Should().BeTrue();
    }
}
