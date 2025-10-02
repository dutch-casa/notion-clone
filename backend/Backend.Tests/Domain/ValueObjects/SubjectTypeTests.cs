using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class SubjectTypeTests
{
    [Theory]
    [InlineData("user")]
    [InlineData("org")]
    [InlineData("role")]
    public void Create_WithValidType_ShouldSucceed(string typeName)
    {
        // Act
        var result = SubjectType.Create(typeName);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(typeName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("group")]
    public void Create_WithInvalidType_ShouldThrowArgumentException(string typeName)
    {
        // Act
        Action act = () => SubjectType.Create(typeName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid subject type*");
    }

    [Fact]
    public void User_Property_ShouldReturnUserType()
    {
        // Act
        var type = SubjectType.User;

        // Assert
        type.Value.Should().Be("user");
    }

    [Fact]
    public void Org_Property_ShouldReturnOrgType()
    {
        // Act
        var type = SubjectType.Org;

        // Assert
        type.Value.Should().Be("org");
    }

    [Fact]
    public void Role_Property_ShouldReturnRoleType()
    {
        // Act
        var type = SubjectType.Role;

        // Assert
        type.Value.Should().Be("role");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var type1 = SubjectType.Create("user");
        var type2 = SubjectType.Create("user");

        // Act & Assert
        type1.Should().Be(type2);
        (type1 == type2).Should().BeTrue();
        type1.GetHashCode().Should().Be(type2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var type1 = SubjectType.Create("user");
        var type2 = SubjectType.Create("org");

        // Act & Assert
        type1.Should().NotBe(type2);
        (type1 == type2).Should().BeFalse();
    }
}
