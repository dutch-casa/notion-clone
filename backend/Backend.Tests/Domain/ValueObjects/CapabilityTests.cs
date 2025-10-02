using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class CapabilityTests
{
    [Theory]
    [InlineData("view")]
    [InlineData("comment")]
    [InlineData("edit")]
    [InlineData("admin")]
    public void Create_WithValidCapability_ShouldSucceed(string capabilityName)
    {
        // Act
        var result = Capability.Create(capabilityName);

        // Assert
        result.Value.Should().Be(capabilityName.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void Create_WithInvalidCapability_ShouldThrowArgumentException(string capabilityName)
    {
        // Act
        Action act = () => Capability.Create(capabilityName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Hierarchy_Admin_ShouldImplyAllLowerCapabilities()
    {
        // Arrange
        var admin = Capability.Admin;

        // Act & Assert
        admin.Implies(Capability.Edit).Should().BeTrue();
        admin.Implies(Capability.Comment).Should().BeTrue();
        admin.Implies(Capability.View).Should().BeTrue();
        admin.Implies(Capability.Admin).Should().BeTrue();
    }

    [Fact]
    public void Hierarchy_Edit_ShouldImplyCommentAndView()
    {
        // Arrange
        var edit = Capability.Edit;

        // Act & Assert
        edit.Implies(Capability.Comment).Should().BeTrue();
        edit.Implies(Capability.View).Should().BeTrue();
        edit.Implies(Capability.Edit).Should().BeTrue();
        edit.Implies(Capability.Admin).Should().BeFalse();
    }

    [Fact]
    public void Hierarchy_Comment_ShouldImplyView()
    {
        // Arrange
        var comment = Capability.Comment;

        // Act & Assert
        comment.Implies(Capability.View).Should().BeTrue();
        comment.Implies(Capability.Comment).Should().BeTrue();
        comment.Implies(Capability.Edit).Should().BeFalse();
        comment.Implies(Capability.Admin).Should().BeFalse();
    }

    [Fact]
    public void Hierarchy_View_ShouldOnlyImplyItself()
    {
        // Arrange
        var view = Capability.View;

        // Act & Assert
        view.Implies(Capability.View).Should().BeTrue();
        view.Implies(Capability.Comment).Should().BeFalse();
        view.Implies(Capability.Edit).Should().BeFalse();
        view.Implies(Capability.Admin).Should().BeFalse();
    }

    [Fact]
    public void Level_ShouldReturnCorrectHierarchyLevel()
    {
        // Assert
        Capability.Admin.Level.Should().Be(4);
        Capability.Edit.Level.Should().Be(3);
        Capability.Comment.Level.Should().Be(2);
        Capability.View.Level.Should().Be(1);
    }
}
