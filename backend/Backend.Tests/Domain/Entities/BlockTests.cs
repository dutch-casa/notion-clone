using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.Entities;

public class BlockTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var pageId = Guid.NewGuid();
        var sortKey = SortKey.Create(1m);
        var type = BlockType.Paragraph;

        // Act
        var block = new Block(pageId, sortKey, type, null, null);

        // Assert
        block.Id.Should().NotBe(Guid.Empty);
        block.PageId.Should().Be(pageId);
        block.SortKey.Should().Be(sortKey);
        block.Type.Should().Be(type);
        block.ParentBlockId.Should().BeNull();
        block.Json.Should().BeNull();
    }

    [Fact]
    public void Create_WithParentBlock_ShouldSucceed()
    {
        // Arrange
        var pageId = Guid.NewGuid();
        var parentBlockId = Guid.NewGuid();
        var sortKey = SortKey.Create(1m);
        var type = BlockType.Paragraph;

        // Act
        var block = new Block(pageId, sortKey, type, parentBlockId, null);

        // Assert
        block.ParentBlockId.Should().Be(parentBlockId);
    }

    [Fact]
    public void Create_WithJson_ShouldSucceed()
    {
        // Arrange
        var pageId = Guid.NewGuid();
        var sortKey = SortKey.Create(1m);
        var type = BlockType.Heading;
        var json = """{"level": 1}""";

        // Act
        var block = new Block(pageId, sortKey, type, null, json);

        // Assert
        block.Json.Should().Be(json);
    }

    [Fact]
    public void Create_WithEmptyPageId_ShouldThrowArgumentException()
    {
        // Arrange
        var sortKey = SortKey.Create(1m);
        var type = BlockType.Paragraph;

        // Act
        Action act = () => new Block(Guid.Empty, sortKey, type, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*PageId*");
    }

    [Fact]
    public void UpdateType_WithValidType_ShouldSucceed()
    {
        // Arrange
        var block = new Block(Guid.NewGuid(), SortKey.Create(1m), BlockType.Paragraph, null, null);
        var newType = BlockType.Heading;

        // Act
        block.UpdateType(newType);

        // Assert
        block.Type.Should().Be(newType);
    }

    [Fact]
    public void UpdateSortKey_WithValidSortKey_ShouldSucceed()
    {
        // Arrange
        var block = new Block(Guid.NewGuid(), SortKey.Create(1m), BlockType.Paragraph, null, null);
        var newSortKey = SortKey.Create(2m);

        // Act
        block.UpdateSortKey(newSortKey);

        // Assert
        block.SortKey.Should().Be(newSortKey);
    }

    [Fact]
    public void UpdateJson_WithValidJson_ShouldSucceed()
    {
        // Arrange
        var block = new Block(Guid.NewGuid(), SortKey.Create(1m), BlockType.Heading, null, null);
        var newJson = """{"level": 2}""";

        // Act
        block.UpdateJson(newJson);

        // Assert
        block.Json.Should().Be(newJson);
    }
}
