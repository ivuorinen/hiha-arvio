using FluentAssertions;
using HihaArvio.Models;

namespace HihaArvio.Tests.Models;

/// <summary>
/// Tests for the <see cref="ShakeData"/> model, verifying construction, property validation, and default values.
/// </summary>
public class ShakeDataTests
{
    /// <summary>
    /// Verifies that all properties are correctly assigned during object initialization.
    /// </summary>
    [Fact]
    public void ShakeData_ShouldCreateWithAllProperties()
    {
        // Arrange
        var intensity = 0.65;
        var duration = TimeSpan.FromSeconds(3);
        var isShaking = true;

        // Act
        var shakeData = new ShakeData
        {
            Intensity = intensity,
            Duration = duration,
            IsShaking = isShaking
        };

        // Assert
        shakeData.Intensity.Should().Be(intensity);
        shakeData.Duration.Should().Be(duration);
        shakeData.IsShaking.Should().Be(isShaking);
    }

    /// <summary>
    /// Verifies that Intensity accepts valid values within the 0.0 to 1.0 range.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void ShakeData_ShouldAcceptValidIntensityValues(double intensity)
    {
        // Act
        var shakeData = new ShakeData { Intensity = intensity };

        // Assert
        shakeData.Intensity.Should().Be(intensity);
    }

    /// <summary>
    /// Verifies that Intensity throws ArgumentOutOfRangeException for values outside 0.0 to 1.0.
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    public void ShakeData_ShouldThrowForInvalidIntensity(double invalidIntensity)
    {
        // Act
        Action act = () => _ = new ShakeData { Intensity = invalidIntensity };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*must be between 0.0 and 1.0*");
    }

    /// <summary>
    /// Verifies that Duration accepts a zero TimeSpan value.
    /// </summary>
    [Fact]
    public void ShakeData_ShouldAcceptZeroDuration()
    {
        // Act
        var shakeData = new ShakeData { Duration = TimeSpan.Zero };

        // Assert
        shakeData.Duration.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that Duration accepts a positive TimeSpan value.
    /// </summary>
    [Fact]
    public void ShakeData_ShouldAcceptPositiveDuration()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(15.5);

        // Act
        var shakeData = new ShakeData { Duration = duration };

        // Assert
        shakeData.Duration.Should().Be(duration);
    }

    /// <summary>
    /// Verifies that IsShaking accepts both true and false values.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShakeData_ShouldAcceptBooleanIsShakingValues(bool isShaking)
    {
        // Act
        var shakeData = new ShakeData { IsShaking = isShaking };

        // Assert
        shakeData.IsShaking.Should().Be(isShaking);
    }

    /// <summary>
    /// Verifies that the default constructor initializes all properties to their default values.
    /// </summary>
    [Fact]
    public void ShakeData_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var shakeData = new ShakeData();

        // Assert
        shakeData.Intensity.Should().Be(0.0);
        shakeData.Duration.Should().Be(TimeSpan.Zero);
        shakeData.IsShaking.Should().BeFalse();
    }
}
