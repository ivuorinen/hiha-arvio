using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

public class ShakeDetectionServiceTests
{
    private readonly IShakeDetectionService _service;

    public ShakeDetectionServiceTests()
    {
        _service = new ShakeDetectionService();
    }

    #region Initialization and State Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultState()
    {
        // Assert
        _service.IsMonitoring.Should().BeFalse();
        _service.CurrentShakeData.Should().NotBeNull();
        _service.CurrentShakeData.IsShaking.Should().BeFalse();
        _service.CurrentShakeData.Intensity.Should().Be(0.0);
        _service.CurrentShakeData.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void StartMonitoring_ShouldSetIsMonitoringToTrue()
    {
        // Act
        _service.StartMonitoring();

        // Assert
        _service.IsMonitoring.Should().BeTrue();
    }

    [Fact]
    public void StopMonitoring_ShouldSetIsMonitoringToFalse()
    {
        // Arrange
        _service.StartMonitoring();

        // Act
        _service.StopMonitoring();

        // Assert
        _service.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void StartMonitoring_MultipleTimes_ShouldRemainsTrue()
    {
        // Act
        _service.StartMonitoring();
        _service.StartMonitoring();
        _service.StartMonitoring();

        // Assert
        _service.IsMonitoring.Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldClearShakeState()
    {
        // Arrange - Simulate shake
        _service.StartMonitoring();
        _service.ProcessAccelerometerReading(3.0, 3.0, 3.0); // Strong shake
        _service.CurrentShakeData.IsShaking.Should().BeTrue();

        // Act
        _service.Reset();

        // Assert
        _service.CurrentShakeData.IsShaking.Should().BeFalse();
        _service.CurrentShakeData.Intensity.Should().Be(0.0);
        _service.CurrentShakeData.Duration.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Accelerometer Processing Tests

    [Fact]
    public void ProcessAccelerometerReading_WhenNotMonitoring_ShouldNotUpdateState()
    {
        // Arrange
        _service.StopMonitoring();

        // Act
        _service.ProcessAccelerometerReading(2.0, 2.0, 2.0);

        // Assert
        _service.CurrentShakeData.IsShaking.Should().BeFalse();
        _service.CurrentShakeData.Intensity.Should().Be(0.0);
    }

    [Fact]
    public void ProcessAccelerometerReading_WithLowAcceleration_ShouldNotDetectShake()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Simulate device at rest (1g gravity)
        _service.ProcessAccelerometerReading(0.0, 0.0, 1.0);

        // Assert
        _service.CurrentShakeData.IsShaking.Should().BeFalse();
        _service.CurrentShakeData.Intensity.Should().Be(0.0);
    }

    [Fact]
    public void ProcessAccelerometerReading_WithModerateShake_ShouldDetectShake()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Simulate moderate shake (3g total acceleration, 2g shake after subtracting 1g gravity)
        _service.ProcessAccelerometerReading(2.0, 1.5, 1.5);

        // Assert
        _service.CurrentShakeData.IsShaking.Should().BeTrue();
        _service.CurrentShakeData.Intensity.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ProcessAccelerometerReading_WithStrongShake_ShouldDetectHighIntensity()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Simulate strong shake (4g total acceleration)
        _service.ProcessAccelerometerReading(3.0, 2.0, 1.5);

        // Assert
        _service.CurrentShakeData.IsShaking.Should().BeTrue();
        _service.CurrentShakeData.Intensity.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void ProcessAccelerometerReading_ShouldNormalizeIntensityBetweenZeroAndOne()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Test various shake strengths
        for (int i = 0; i < 50; i++)
        {
            var x = Random.Shared.NextDouble() * 6.0 - 3.0;
            var y = Random.Shared.NextDouble() * 6.0 - 3.0;
            var z = Random.Shared.NextDouble() * 6.0 - 3.0;
            _service.ProcessAccelerometerReading(x, y, z);

            // Assert
            _service.CurrentShakeData.Intensity.Should().BeInRange(0.0, 1.0);
        }
    }

    #endregion

    #region Shake Duration Tests

    [Fact]
    public void ProcessAccelerometerReading_DuringShake_ShouldIncreaseDuration()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Simulate continuous shake
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);
        var initialDuration = _service.CurrentShakeData.Duration;

        Thread.Sleep(100); // Wait 100ms
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);
        var laterDuration = _service.CurrentShakeData.Duration;

        // Assert
        laterDuration.Should().BeGreaterThan(initialDuration);
    }

    [Fact]
    public void ProcessAccelerometerReading_AfterShakeStops_ShouldResetDuration()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Start shake
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);
        _service.CurrentShakeData.IsShaking.Should().BeTrue();
        Thread.Sleep(50);
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);
        var shakeDuration = _service.CurrentShakeData.Duration;
        shakeDuration.Should().BeGreaterThan(TimeSpan.Zero);

        // Stop shake
        Thread.Sleep(50);
        _service.ProcessAccelerometerReading(0.0, 0.0, 1.0);
        _service.CurrentShakeData.IsShaking.Should().BeFalse();

        // Start new shake
        Thread.Sleep(50);
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);

        // Assert - Duration should reset for new shake
        _service.CurrentShakeData.Duration.Should().BeLessThan(shakeDuration);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ShakeDataChanged_ShouldFireWhenShakeStarts()
    {
        // Arrange
        _service.StartMonitoring();
        ShakeData? capturedData = null;
        _service.ShakeDataChanged += (sender, data) => capturedData = data;

        // Act
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);

        // Assert
        capturedData.Should().NotBeNull();
        capturedData!.IsShaking.Should().BeTrue();
        capturedData.Intensity.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ShakeDataChanged_ShouldFireWhenShakeStops()
    {
        // Arrange
        _service.StartMonitoring();
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5); // Start shake

        var eventFiredCount = 0;
        ShakeData? lastCapturedData = null;
        _service.ShakeDataChanged += (sender, data) =>
        {
            eventFiredCount++;
            lastCapturedData = data;
        };

        // Act - Stop shake
        _service.ProcessAccelerometerReading(0.0, 0.0, 1.0);

        // Assert
        eventFiredCount.Should().BeGreaterThan(0);
        lastCapturedData.Should().NotBeNull();
        lastCapturedData!.IsShaking.Should().BeFalse();
    }

    [Fact]
    public void ShakeDataChanged_ShouldFireWhenIntensityChanges()
    {
        // Arrange
        _service.StartMonitoring();

        var intensities = new List<double>();
        _service.ShakeDataChanged += (sender, data) => intensities.Add(data.Intensity);

        // Act - Start with gentle shake, then increase intensity
        _service.ProcessAccelerometerReading(2.0, 1.5, 1.0); // Gentle shake (~2.5g total, ~1.5g shake)
        _service.ProcessAccelerometerReading(3.0, 2.5, 2.0); // Strong shake (~4.4g total, ~3.4g shake)

        // Assert
        intensities.Should().HaveCountGreaterThanOrEqualTo(2);
        intensities.Last().Should().BeGreaterThan(intensities.First());
    }

    [Fact]
    public void ShakeDataChanged_WhenNotMonitoring_ShouldNotFire()
    {
        // Arrange
        _service.StopMonitoring();
        var eventFired = false;
        _service.ShakeDataChanged += (sender, data) => eventFired = true;

        // Act
        _service.ProcessAccelerometerReading(2.5, 2.0, 1.5);

        // Assert
        eventFired.Should().BeFalse();
    }

    #endregion

    #region Intensity Calculation Tests

    [Fact]
    public void ProcessAccelerometerReading_ShouldCalculateIntensityFromMagnitude()
    {
        // Arrange
        _service.StartMonitoring();

        // Act & Assert - Test known values
        // Magnitude = sqrt(3^2 + 4^2 + 0^2) = 5.0
        _service.ProcessAccelerometerReading(3.0, 4.0, 0.0);
        _service.CurrentShakeData.IsShaking.Should().BeTrue();
        _service.CurrentShakeData.Intensity.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void ProcessAccelerometerReading_WithZeroAcceleration_ShouldHaveZeroIntensity()
    {
        // Arrange
        _service.StartMonitoring();

        // Act
        _service.ProcessAccelerometerReading(0.0, 0.0, 0.0);

        // Assert
        _service.CurrentShakeData.Intensity.Should().Be(0.0);
        _service.CurrentShakeData.IsShaking.Should().BeFalse();
    }

    [Fact]
    public void ProcessAccelerometerReading_WithNegativeValues_ShouldCalculateCorrectMagnitude()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Negative values should be squared, making them positive
        _service.ProcessAccelerometerReading(-2.0, -2.0, -2.0);

        // Assert
        _service.CurrentShakeData.IsShaking.Should().BeTrue();
        _service.CurrentShakeData.Intensity.Should().BeGreaterThan(0.0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Reset_WhenNotShaking_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _service.Reset();
        act.Should().NotThrow();
    }

    [Fact]
    public void StopMonitoring_WhenNotStarted_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _service.StopMonitoring();
        act.Should().NotThrow();
    }

    [Fact]
    public void ProcessAccelerometerReading_WithExtremeValues_ShouldNotThrow()
    {
        // Arrange
        _service.StartMonitoring();

        // Act & Assert
        var act = () =>
        {
            _service.ProcessAccelerometerReading(100.0, 100.0, 100.0);
            _service.ProcessAccelerometerReading(-100.0, -100.0, -100.0);
            _service.ProcessAccelerometerReading(double.MaxValue / 1000, 0, 0);
        };
        act.Should().NotThrow();

        // Intensity should still be normalized
        _service.CurrentShakeData.Intensity.Should().BeInRange(0.0, 1.0);
    }

    #endregion
}
