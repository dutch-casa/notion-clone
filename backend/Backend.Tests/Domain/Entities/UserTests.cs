using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var name = "John Doe";
        var passwordHash = "hashedpassword123";

        // Act
        var user = new User(email, name, passwordHash);

        // Assert
        user.Id.Should().NotBe(Guid.Empty);
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.PasswordHash.Should().Be(passwordHash);
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var passwordHash = "hashedpassword123";

        // Act
        Action act = () => new User(email, name, passwordHash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyPasswordHash_ShouldThrowArgumentException(string passwordHash)
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var name = "John Doe";

        // Act
        Action act = () => new User(email, name, passwordHash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Password hash*");
    }
}
