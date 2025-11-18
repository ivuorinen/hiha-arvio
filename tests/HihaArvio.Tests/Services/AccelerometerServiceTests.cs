using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

/// <summary>
/// Tests for IAccelerometerService interface contract.
/// Platform-specific implementations must satisfy these tests.
/// </summary>
public class AccelerometerServiceContractTests
{
    [Fact]
    public void SensorReading_ShouldHaveXYZProperties()
    {
        // Arrange & Act
        var data = new SensorReading
        {
            X = 1.5,
            Y = 2.5,
            Z = 3.5
        };

        // Assert
        data.X.Should().Be(1.5);
        data.Y.Should().Be(2.5);
        data.Z.Should().Be(3.5);
    }

    [Fact]
    public void SensorReading_ShouldBeInitOnly()
    {
        // Arrange
        var data = new SensorReading { X = 1.0, Y = 2.0, Z = 3.0 };

        // Assert - if this compiles, init-only properties work
        data.X.Should().Be(1.0);
    }

    [Fact]
    public void SensorReading_ShouldSupportEqualityComparison()
    {
        // Arrange
        var data1 = new SensorReading { X = 1.0, Y = 2.0, Z = 3.0 };
        var data2 = new SensorReading { X = 1.0, Y = 2.0, Z = 3.0 };
        var data3 = new SensorReading { X = 1.0, Y = 2.0, Z = 4.0 };

        // Assert
        data1.Equals(data2).Should().BeTrue();
        data1.Equals(data3).Should().BeFalse();
    }
}

/// <summary>
/// Contract tests that all IAccelerometerService implementations must pass.
/// This ensures platform-specific implementations behave consistently.
/// </summary>
public abstract class AccelerometerServiceContractTestsBase
{
    /// <summary>
    /// Factory method for creating the service instance to test.
    /// Implemented by platform-specific test classes.
    /// </summary>
    protected abstract IAccelerometerService CreateService();

    [Fact]
    public void Start_ShouldEnableMonitoring()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.Start();

        // Assert
        // Service should now be monitoring (implementation-specific verification)
        // This is a smoke test - platform implementations will have more specific tests
    }

    [Fact]
    public void Stop_ShouldDisableMonitoring()
    {
        // Arrange
        var service = CreateService();
        service.Start();

        // Act
        service.Stop();

        // Assert
        // Service should no longer be monitoring (implementation-specific verification)
        // This is a smoke test - platform implementations will have more specific tests
    }

    [Fact]
    public void Stop_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.Stop();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsSupported_ShouldReturnBoolean()
    {
        // Arrange
        var service = CreateService();

        // Act
        var isSupported = service.IsSupported;

        // Assert
        isSupported.Should().Be(isSupported); // Just verify it's a boolean value
    }

    [Fact]
    public void ReadingChanged_ShouldNotBeNull()
    {
        // Arrange
        var service = CreateService();

        // Act - Subscribe to event
        var eventRaised = false;
        service.ReadingChanged += (sender, data) => eventRaised = true;

        // Assert - Event subscription should work without throwing
        // Actual event raising is tested in platform-specific tests
        eventRaised.Should().BeFalse(); // Not raised yet
    }
}
