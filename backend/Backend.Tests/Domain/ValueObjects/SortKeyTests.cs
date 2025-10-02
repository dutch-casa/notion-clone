using Backend.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Domain.ValueObjects;

public class SortKeyTests
{
    [Fact]
    public void Create_WithValidDecimal_ShouldSucceed()
    {
        // Arrange
        var value = 1.5m;

        // Act
        var result = SortKey.Create(value);

        // Assert
        result.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Create_WithNonPositiveValue_ShouldThrowArgumentException(decimal value)
    {
        // Act
        Action act = () => SortKey.Create(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    [Fact]
    public void Between_WithNullBefore_ShouldReturnHalfOfAfter()
    {
        // Arrange
        var after = SortKey.Create(10m);

        // Act
        var result = SortKey.Between(null, after);

        // Assert
        result.Value.Should().Be(5m);
    }

    [Fact]
    public void Between_WithNullAfter_ShouldReturnBeforePlusOne()
    {
        // Arrange
        var before = SortKey.Create(10m);

        // Act
        var result = SortKey.Between(before, null);

        // Assert
        result.Value.Should().Be(11m);
    }

    [Fact]
    public void Between_WithBothNull_ShouldReturnDefaultValue()
    {
        // Act
        var result = SortKey.Between(null, null);

        // Assert
        result.Value.Should().Be(1m);
    }

    [Fact]
    public void Between_WithTwoValues_ShouldReturnMidpoint()
    {
        // Arrange
        var before = SortKey.Create(1m);
        var after = SortKey.Create(2m);

        // Act
        var result = SortKey.Between(before, after);

        // Assert
        result.Value.Should().Be(1.5m);
    }

    [Fact]
    public void Between_WithFractionalValues_ShouldReturnMidpoint()
    {
        // Arrange
        var before = SortKey.Create(1.5m);
        var after = SortKey.Create(1.6m);

        // Act
        var result = SortKey.Between(before, after);

        // Assert
        result.Value.Should().Be(1.55m);
    }

    [Fact]
    public void Between_RepeatedInsertions_ShouldMaintainOrdering()
    {
        // Arrange
        var first = SortKey.Create(1m);
        var last = SortKey.Create(2m);

        // Act - Insert multiple items between first and last
        var insert1 = SortKey.Between(first, last);   // 1.5
        var insert2 = SortKey.Between(first, insert1); // 1.25
        var insert3 = SortKey.Between(insert1, last);  // 1.75

        // Assert - All values should be in correct order
        first.Value.Should().BeLessThan(insert2.Value);
        insert2.Value.Should().BeLessThan(insert1.Value);
        insert1.Value.Should().BeLessThan(insert3.Value);
        insert3.Value.Should().BeLessThan(last.Value);
    }

    [Fact]
    public void CompareTo_ShouldOrderCorrectly()
    {
        // Arrange
        var small = SortKey.Create(1m);
        var medium = SortKey.Create(1.5m);
        var large = SortKey.Create(2m);

        // Act & Assert
        small.CompareTo(medium).Should().BeLessThan(0);
        medium.CompareTo(large).Should().BeLessThan(0);
        large.CompareTo(small).Should().BeGreaterThan(0);
        small.CompareTo(small).Should().Be(0);
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkCorrectly()
    {
        // Arrange
        var small = SortKey.Create(1m);
        var large = SortKey.Create(2m);

        // Act & Assert
        (small < large).Should().BeTrue();
        (large > small).Should().BeTrue();
        (small <= large).Should().BeTrue();
        (large >= small).Should().BeTrue();
        (small == large).Should().BeFalse();
        (small != large).Should().BeTrue();
    }

    [Fact]
    public void First_Property_ShouldReturnDefaultStartValue()
    {
        // Act
        var first = SortKey.First;

        // Assert
        first.Value.Should().Be(1m);
    }

    [Theory]
    [InlineData(1000000000.123456789)] // Max value
    public void Create_WithMaxPrecision_ShouldSucceed(double value)
    {
        // Act
        var result = SortKey.Create((decimal)value);

        // Assert
        result.Value.Should().BeApproximately((decimal)value, 0.000000001m);
    }
}
