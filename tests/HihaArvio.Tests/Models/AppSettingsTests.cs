using FluentAssertions;
using HihaArvio.Models;

namespace HihaArvio.Tests.Models;

/// <summary>
/// Tests for the <see cref="AppSettings"/> model, verifying default values, property assignment, and validation.
/// </summary>
public class AppSettingsTests
{
    /// <summary>
    /// Verifies that the default constructor sets SelectedMode to Work.
    /// </summary>
    [Fact]
    public void AppSettings_DefaultConstructor_ShouldSetWorkModeAsDefault()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.SelectedMode.Should().Be(EstimateMode.Work);
    }

    /// <summary>
    /// Verifies that the default constructor sets MaxHistorySize to 10.
    /// </summary>
    [Fact]
    public void AppSettings_DefaultConstructor_ShouldSetMaxHistorySizeTo10()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.MaxHistorySize.Should().Be(10);
    }

    /// <summary>
    /// Verifies that SelectedMode can be changed to any valid EstimateMode value.
    /// </summary>
    [Theory]
    [InlineData(EstimateMode.Work)]
    [InlineData(EstimateMode.Generic)]
    [InlineData(EstimateMode.Humorous)]
    public void AppSettings_ShouldAllowModeChanges(EstimateMode mode)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.SelectedMode = mode;

        // Assert
        settings.SelectedMode.Should().Be(mode);
    }

    /// <summary>
    /// Verifies that MaxHistorySize accepts valid positive integer values.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void AppSettings_ShouldAcceptValidMaxHistorySizeValues(int size)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.MaxHistorySize = size;

        // Assert
        settings.MaxHistorySize.Should().Be(size);
    }

    /// <summary>
    /// Verifies that MaxHistorySize throws ArgumentOutOfRangeException for zero or negative values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void AppSettings_ShouldThrowForInvalidMaxHistorySize(int invalidSize)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        Action act = () => settings.MaxHistorySize = invalidSize;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*must be greater than 0*");
    }

    /// <summary>
    /// Verifies that a newly created AppSettings instance has all expected default values.
    /// </summary>
    [Fact]
    public void AppSettings_ShouldCreateWithDefaultValues()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.SelectedMode.Should().Be(EstimateMode.Work);
        settings.MaxHistorySize.Should().Be(10);
    }
}
