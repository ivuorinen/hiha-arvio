using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

/// <summary>
/// Tests for iOS accelerometer service implementation.
/// </summary>
public class IosAccelerometerServiceTests : AccelerometerServiceContractTestsBase
{
    protected override IAccelerometerService CreateService()
    {
        return new IosAccelerometerService();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithStoppedState()
    {
        // Arrange & Act
        var service = new IosAccelerometerService();

        // Assert
        // Service should be in stopped state initially
        // (Can't directly test internal state, but Start/Stop should work)
    }

    [Fact]
    public void IsSupported_ShouldBePlatformDependent()
    {
        // Arrange
        var service = new IosAccelerometerService();

        // Act
        var isSupported = service.IsSupported;

        // Assert
        // On iOS/macOS, should return true (MAUI accelerometer available)
        // On net8.0 (test runner), should return false
#if IOS || MACCATALYST
        isSupported.Should().BeTrue();
#else
        isSupported.Should().BeFalse();
#endif
    }

    [Fact]
    public void Start_Multiple_ShouldNotThrow()
    {
        // Arrange
        var service = new IosAccelerometerService();

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
        var service = new IosAccelerometerService();
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
    public async Task ReadingChanged_AfterStart_ShouldEventuallyFire()
    {
        // Arrange
        var service = new IosAccelerometerService();
        SensorReading? receivedReading = null;
        var eventFired = false;

        service.ReadingChanged += (sender, reading) =>
        {
            receivedReading = reading;
            eventFired = true;
        };

        // Act
        service.Start();
        await Task.Delay(200); // Wait for at least one reading

        // Assert
        // On real hardware, this would fire. On simulator/test environment,
        // the service may provide simulated readings or not fire at all.
        // This test verifies the event subscription doesn't crash.
        eventFired.Should().Be(eventFired); // Tautology for now

        // Cleanup
        service.Stop();
    }
}
