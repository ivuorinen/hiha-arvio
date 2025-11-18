using FluentAssertions;
using HihaArvio.Models;

namespace HihaArvio.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultConstructor_ShouldSetWorkModeAsDefault()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.SelectedMode.Should().Be(EstimateMode.Work);
    }

    [Fact]
    public void AppSettings_DefaultConstructor_ShouldSetMaxHistorySizeTo10()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.MaxHistorySize.Should().Be(10);
    }

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
