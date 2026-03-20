using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;
using NSubstitute;

namespace HihaArvio.Tests.Services;

/// <summary>
/// Tests for the ShakeDetectionService, covering initialization, accelerometer processing,
/// shake duration tracking, event notifications, intensity calculations, and edge cases.
/// </summary>
public class ShakeDetectionServiceTests
{
    private readonly IAccelerometerService _mockAccelerometer;
    private readonly IShakeDetectionService _service;

    public ShakeDetectionServiceTests()
    {
        _mockAccelerometer = Substitute.For<IAccelerometerService>();
        _service = new ShakeDetectionService(_mockAccelerometer);
    }

    #region Initialization and State Tests

    /// <summary>
    /// Verifies that a new service instance starts with monitoring disabled and default shake data.
    /// </summary>
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

    /// <summary>
    /// Verifies that StartMonitoring sets the IsMonitoring flag to true.
    /// </summary>
    [Fact]
    public void StartMonitoring_ShouldSetIsMonitoringToTrue()
    {
        // Act
        _service.StartMonitoring();

        // Assert
        _service.IsMonitoring.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that StopMonitoring sets the IsMonitoring flag to false.
    /// </summary>
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

    /// <summary>
    /// Verifies that calling StartMonitoring multiple times keeps IsMonitoring true.
    /// </summary>
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

    /// <summary>
    /// Verifies that Reset clears the current shake state back to defaults.
    /// </summary>
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

    /// <summary>
    /// Verifies that accelerometer readings are ignored when monitoring is not active.
    /// </summary>
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

    /// <summary>
    /// Verifies that low acceleration (device at rest) does not trigger shake detection.
    /// </summary>
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

    /// <summary>
    /// Verifies that moderate acceleration triggers shake detection with positive intensity.
    /// </summary>
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

    /// <summary>
    /// Verifies that strong acceleration results in high shake intensity above 0.5.
    /// </summary>
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

    /// <summary>
    /// Verifies that intensity is always normalized to the [0.0, 1.0] range regardless of input values.
    /// </summary>
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

    /// <summary>
    /// Verifies that shake duration increases as continuous shake readings are processed over time.
    /// </summary>
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

    /// <summary>
    /// Verifies that shake duration resets when a new shake begins after a previous shake stopped.
    /// </summary>
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

    /// <summary>
    /// Verifies that the ShakeDataChanged event fires with IsShaking true when a shake begins.
    /// </summary>
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

    /// <summary>
    /// Verifies that the ShakeDataChanged event fires with IsShaking false when a shake ends.
    /// </summary>
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

    /// <summary>
    /// Verifies that the ShakeDataChanged event fires with increasing intensity values as shake strengthens.
    /// </summary>
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

    /// <summary>
    /// Verifies that the ShakeDataChanged event does not fire when monitoring is disabled.
    /// </summary>
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

    /// <summary>
    /// Verifies that intensity is correctly derived from the vector magnitude of accelerometer readings.
    /// </summary>
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

    /// <summary>
    /// Verifies that zero acceleration produces zero intensity and no shake detection.
    /// </summary>
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

    /// <summary>
    /// Verifies that negative accelerometer values are handled correctly via squaring in magnitude calculation.
    /// </summary>
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

    #region Peak Intensity Tests

    /// <summary>
    /// Verifies that the service reports peak intensity rather than instantaneous intensity during a shake.
    /// Per spec §3.1.1: Track peak magnitude during entire shake session.
    /// </summary>
    [Fact]
    public void ProcessAccelerometerReading_DuringShake_ShouldReportPeakIntensity()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Start with strong shake
        _service.ProcessAccelerometerReading(3.0, 3.0, 3.0); // High intensity
        var peakIntensity = _service.CurrentShakeData.Intensity;

        // Then reduce shake strength
        _service.ProcessAccelerometerReading(2.0, 1.5, 1.5); // Lower intensity

        // Assert - Should still report the peak intensity
        _service.CurrentShakeData.Intensity.Should().Be(peakIntensity,
            "intensity should remain at peak during a shake session");
        _service.CurrentShakeData.IsShaking.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that peak intensity increases when a stronger reading arrives.
    /// </summary>
    [Fact]
    public void ProcessAccelerometerReading_WithIncreasingShake_ShouldUpdatePeak()
    {
        // Arrange
        _service.StartMonitoring();

        // Act - Start with moderate shake
        _service.ProcessAccelerometerReading(2.0, 1.5, 1.5);
        var firstPeak = _service.CurrentShakeData.Intensity;

        // Then increase shake strength
        _service.ProcessAccelerometerReading(3.0, 3.0, 3.0);
        var secondPeak = _service.CurrentShakeData.Intensity;

        // Assert - Peak should increase
        secondPeak.Should().BeGreaterThan(firstPeak,
            "peak should increase with stronger readings");
    }

    /// <summary>
    /// Verifies that peak intensity resets when a new shake session begins.
    /// </summary>
    [Fact]
    public void ProcessAccelerometerReading_NewShakeSession_ShouldResetPeak()
    {
        // Arrange
        _service.StartMonitoring();

        // First shake session with high intensity
        _service.ProcessAccelerometerReading(3.0, 3.0, 3.0);
        var firstPeak = _service.CurrentShakeData.Intensity;
        firstPeak.Should().BeGreaterThan(0.5);

        // Stop shaking
        _service.ProcessAccelerometerReading(0.0, 0.0, 1.0);
        _service.CurrentShakeData.IsShaking.Should().BeFalse();

        // Start new shake with moderate intensity
        _service.ProcessAccelerometerReading(2.0, 1.5, 1.5);

        // Assert - Peak should be based on new session, not carry over
        _service.CurrentShakeData.Intensity.Should().BeLessThan(firstPeak,
            "peak should reset for new shake session");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Verifies that calling Reset when not shaking does not throw an exception.
    /// </summary>
    [Fact]
    public void Reset_WhenNotShaking_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _service.Reset();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that calling StopMonitoring without prior StartMonitoring does not throw.
    /// </summary>
    [Fact]
    public void StopMonitoring_WhenNotStarted_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _service.StopMonitoring();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that extreme accelerometer values are handled gracefully without exceptions.
    /// </summary>
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
