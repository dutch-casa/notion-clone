using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class ResourceTypeTests
{
    [Theory]
    [InlineData("org")]
    [InlineData("page")]
    [InlineData("file")]
    public void Create_WithValidType_ShouldSucceed(string typeName)
    {
        // Act
        var result = ResourceType.Create(typeName);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(typeName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("user")]
    public void Create_WithInvalidType_ShouldThrowArgumentException(string typeName)
    {
        // Act
        Action act = () => ResourceType.Create(typeName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid resource type*");
    }

    [Fact]
    public void Org_Property_ShouldReturnOrgType()
    {
        // Act
        var type = ResourceType.Org;

        // Assert
        type.Value.Should().Be("org");
    }

    [Fact]
    public void Page_Property_ShouldReturnPageType()
    {
        // Act
        var type = ResourceType.Page;

        // Assert
        type.Value.Should().Be("page");
    }

    [Fact]
    public void File_Property_ShouldReturnFileType()
    {
        // Act
        var type = ResourceType.File;

        // Assert
        type.Value.Should().Be("file");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var type1 = ResourceType.Create("org");
        var type2 = ResourceType.Create("org");

        // Act & Assert
        type1.Should().Be(type2);
        (type1 == type2).Should().BeTrue();
        type1.GetHashCode().Should().Be(type2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var type1 = ResourceType.Create("org");
        var type2 = ResourceType.Create("page");

        // Act & Assert
        type1.Should().NotBe(type2);
        (type1 == type2).Should().BeFalse();
    }
}
