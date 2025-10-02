using Backend.Domain.Aggregates;
using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.Aggregates;

public class PageTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var title = "My Page";

        // Act
        var page = new Page(orgId, title, createdBy);

        // Assert
        page.Id.Should().NotBe(Guid.Empty);
        page.OrgId.Should().Be(orgId);
        page.Title.Should().Be(title);
        page.CreatedBy.Should().Be(createdBy);
        page.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        page.Blocks.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldThrowArgumentException(string title)
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        Action act = () => new Page(orgId, title, createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title*");
    }

    [Fact]
    public void Create_WithEmptyOrgId_ShouldThrowArgumentException()
    {
        // Arrange
        var createdBy = Guid.NewGuid();

        // Act
        Action act = () => new Page(Guid.Empty, "Title", createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*OrgId*");
    }

    [Fact]
    public void Create_WithEmptyCreatedBy_ShouldThrowArgumentException()
    {
        // Arrange
        var orgId = Guid.NewGuid();

        // Act
        Action act = () => new Page(orgId, "Title", Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CreatedBy*");
    }

    [Fact]
    public void ChangeTitle_WithValidTitle_ShouldSucceed()
    {
        // Arrange
        var page = new Page(Guid.NewGuid(), "Original Title", Guid.NewGuid());
        var newTitle = "Updated Title";
        var changedBy = Guid.NewGuid();

        // Act
        page.ChangeTitle(newTitle, changedBy);

        // Assert
        page.Title.Should().Be(newTitle);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ChangeTitle_WithEmptyTitle_ShouldThrowArgumentException(string newTitle)
    {
        // Arrange
        var page = new Page(Guid.NewGuid(), "Original Title", Guid.NewGuid());

        // Act
        Action act = () => page.ChangeTitle(newTitle, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title*");
    }

    [Fact]
    public void AddBlock_WithValidData_ShouldSucceed()
    {
        // Arrange
        var page = new Page(Guid.NewGuid(), "My Page", Guid.NewGuid());
        var sortKey = SortKey.First;
        var type = BlockType.Paragraph;

        // Act
        var block = page.AddBlock(sortKey, type, null, null);

        // Assert
        block.Should().NotBeNull();
        block.PageId.Should().Be(page.Id);
        block.SortKey.Should().Be(sortKey);
        block.Type.Should().Be(type);
        page.Blocks.Should().ContainSingle();
        page.Blocks.Should().Contain(block);
    }

    [Fact]
    public void RemoveBlock_WithExistingBlock_ShouldSucceed()
    {
        // Arrange
        var page = new Page(Guid.NewGuid(), "My Page", Guid.NewGuid());
        var block = page.AddBlock(SortKey.First, BlockType.Paragraph, null, null);

        // Act
        page.RemoveBlock(block.Id);

        // Assert
        page.Blocks.Should().BeEmpty();
    }

    [Fact]
    public void RemoveBlock_WithNonExistentBlock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var page = new Page(Guid.NewGuid(), "My Page", Guid.NewGuid());
        var nonExistentId = Guid.NewGuid();

        // Act
        Action act = () => page.RemoveBlock(nonExistentId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }
}
