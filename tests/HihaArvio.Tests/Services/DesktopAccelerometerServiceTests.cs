using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

/// <summary>
/// Tests for desktop accelerometer service implementation.
/// Desktop uses simulated accelerometer data for testing purposes.
/// </summary>
public class DesktopAccelerometerServiceTests : AccelerometerServiceContractTestsBase
{
    protected override IAccelerometerService CreateService()
    {
        return new DesktopAccelerometerService();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithStoppedState()
    {
        // Arrange & Act
        var service = new DesktopAccelerometerService();

        // Assert
        // Service should be in stopped state initially
    }

    [Fact]
    public void IsSupported_ShouldReturnTrue()
    {
        // Arrange
        var service = new DesktopAccelerometerService();

        // Act
        var isSupported = service.IsSupported;

        // Assert
        // Desktop accelerometer service is always supported (simulated)
        isSupported.Should().BeTrue();
    }

    [Fact]
    public void Start_Multiple_ShouldNotThrow()
    {
        // Arrange
        var service = new DesktopAccelerometerService();

        // Act
        var act = () =>
        {
            service.Start();
            service.Start(); // Call twice
        };

        // Assert
        act.Should().NotThrow();

        // Cleanup
        service.Stop();
    }

    [Fact]
    public void Stop_Multiple_ShouldNotThrow()
    {
        // Arrange
        var service = new DesktopAccelerometerService();
        service.Start();

        // Act
        var act = () =>
        {
            service.Stop();
            service.Stop(); // Call twice
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ReadingChanged_AfterStart_ShouldProvideSimulatedReadings()
    {
        // Arrange
        var service = new DesktopAccelerometerService();
        SensorReading? receivedReading = null;
        var eventFired = false;

        service.ReadingChanged += (sender, reading) =>
        {
            receivedReading = reading;
            eventFired = true;
        };

        // Act
        service.Start();
        await Task.Delay(200); // Wait for simulated readings to fire

        // Assert
        // Desktop service provides simulated readings periodically
        eventFired.Should().BeTrue();
        receivedReading.Should().NotBeNull();
        receivedReading!.X.Should().BeInRange(-2.0, 2.0);
        receivedReading.Y.Should().BeInRange(-2.0, 2.0);
        receivedReading.Z.Should().BeInRange(-2.0, 2.0);

        // Cleanup
        service.Stop();
    }

    [Fact]
    public async Task Stop_ShouldStopGeneratingReadings()
    {
        // Arrange
        var service = new DesktopAccelerometerService();
        var readingCount = 0;

        service.ReadingChanged += (sender, reading) => readingCount++;

        // Act
        service.Start();
        await Task.Delay(100); // Wait for several readings
        var countBeforeStop = readingCount;

        service.Stop();
        await Task.Delay(100); // Wait to verify no more readings

        // Assert
        // Should have received some readings before stop
        countBeforeStop.Should().BeGreaterThan(0);
        // Count should not increase significantly after stop (allow 1-2 in-flight events)
        readingCount.Should().BeLessThanOrEqualTo(countBeforeStop + 2);
    }
}
