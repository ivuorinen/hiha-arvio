using FluentAssertions;
using HihaArvio.Models;

namespace HihaArvio.Tests.Models;

public class EstimateResultTests
{
    [Fact]
    public void EstimateResult_ShouldCreateWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var estimateText = "2 weeks";
        var mode = EstimateMode.Work;
        var intensity = 0.75;
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var result = new EstimateResult
        {
            Id = id,
            Timestamp = timestamp,
            EstimateText = estimateText,
            Mode = mode,
            ShakeIntensity = intensity,
            ShakeDuration = duration
        };

        // Assert
        result.Id.Should().Be(id);
        result.Timestamp.Should().Be(timestamp);
        result.EstimateText.Should().Be(estimateText);
        result.Mode.Should().Be(mode);
        result.ShakeIntensity.Should().Be(intensity);
        result.ShakeDuration.Should().Be(duration);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(0.7)]
    [InlineData(1.0)]
    public void EstimateResult_ShouldAcceptValidIntensityValues(double intensity)
    {
        // Act
        var result = new EstimateResult { ShakeIntensity = intensity };

        // Assert
        result.ShakeIntensity.Should().Be(intensity);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void EstimateResult_ShouldThrowForInvalidIntensity(double invalidIntensity)
    {
        // Act
        Action act = () => _ = new EstimateResult { ShakeIntensity = invalidIntensity };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*must be between 0.0 and 1.0*");
    }

    [Fact]
    public void EstimateResult_ShouldThrowForNullEstimateText()
    {
        // Act
        Action act = () => _ = new EstimateResult { EstimateText = null! };

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EstimateResult_ShouldThrowForEmptyOrWhitespaceEstimateText(string invalidText)
    {
        // Act
        Action act = () => _ = new EstimateResult { EstimateText = invalidText };

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty or whitespace*");
    }

    [Fact]
    public void EstimateResult_ShouldAcceptZeroDuration()
    {
        // Act
        var result = new EstimateResult { ShakeDuration = TimeSpan.Zero };

        // Assert
        result.ShakeDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void EstimateResult_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var result1 = EstimateResult.Create("test1", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(1));
        var result2 = EstimateResult.Create("test2", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(1));

        // Assert
        result1.Id.Should().NotBe(result2.Id);
        result1.Id.Should().NotBeEmpty();
        result2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void EstimateResult_Create_ShouldSetTimestampAutomatically()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = EstimateResult.Create("test", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(1));
        var after = DateTimeOffset.UtcNow;

        // Assert
        result.Timestamp.Should().BeOnOrAfter(before);
        result.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void EstimateResult_Create_ShouldSetAllProperties()
    {
        // Arrange
        var estimateText = "3 months";
        var mode = EstimateMode.Generic;
        var intensity = 0.8;
        var duration = TimeSpan.FromSeconds(10);

        // Act
        var result = EstimateResult.Create(estimateText, mode, intensity, duration);

        // Assert
        result.EstimateText.Should().Be(estimateText);
        result.Mode.Should().Be(mode);
        result.ShakeIntensity.Should().Be(intensity);
        result.ShakeDuration.Should().Be(duration);
    }
}
