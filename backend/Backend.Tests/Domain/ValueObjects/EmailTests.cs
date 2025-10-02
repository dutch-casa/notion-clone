using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("alice+tag@company.com")]
    public void Create_WithValidEmail_ShouldSucceed(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ShouldThrowArgumentException(string email)
    {
        // Act
        Action act = () => Email.Create(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email required*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Create_WithInvalidFormat_ShouldThrowArgumentException(string email)
    {
        // Act
        Action act = () => Email.Create(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Fact]
    public void Create_ShouldNormalizeToLowercase()
    {
        // Arrange
        var email = "User@Example.COM";

        // Act
        var result = Email.Create(email);

        // Assert
        result.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var email = "  user@example.com  ";

        // Act
        var result = Email.Create(email);

        // Assert
        result.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("user@example.com");

        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var email1 = Email.Create("user1@example.com");
        var email2 = Email.Create("user2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
        (email1 == email2).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act
        string value = email;

        // Assert
        value.Should().Be("user@example.com");
    }
}
