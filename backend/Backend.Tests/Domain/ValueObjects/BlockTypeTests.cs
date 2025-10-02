using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class BlockTypeTests
{
    [Theory]
    [InlineData("paragraph")]
    [InlineData("heading")]
    [InlineData("todo")]
    [InlineData("file")]
    [InlineData("image")]
    public void Create_WithValidType_ShouldSucceed(string typeName)
    {
        // Act
        var result = BlockType.Create(typeName);

        // Assert
        result.Value.Should().Be(typeName.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("video")]
    public void Create_WithInvalidType_ShouldThrowArgumentException(string typeName)
    {
        // Act
        Action act = () => BlockType.Create(typeName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Paragraph_Property_ShouldReturnParagraphType()
    {
        // Act
        var type = BlockType.Paragraph;

        // Assert
        type.Value.Should().Be("paragraph");
        type.IsParagraph.Should().BeTrue();
    }

    [Fact]
    public void Heading_Property_ShouldReturnHeadingType()
    {
        // Act
        var type = BlockType.Heading;

        // Assert
        type.Value.Should().Be("heading");
        type.IsHeading.Should().BeTrue();
    }

    [Fact]
    public void Todo_Property_ShouldReturnTodoType()
    {
        // Act
        var type = BlockType.Todo;

        // Assert
        type.Value.Should().Be("todo");
        type.IsTodo.Should().BeTrue();
    }

    [Fact]
    public void File_Property_ShouldReturnFileType()
    {
        // Act
        var type = BlockType.File;

        // Assert
        type.Value.Should().Be("file");
        type.IsFile.Should().BeTrue();
    }

    [Fact]
    public void Image_Property_ShouldReturnImageType()
    {
        // Act
        var type = BlockType.Image;

        // Assert
        type.Value.Should().Be("image");
        type.IsImage.Should().BeTrue();
    }
}
