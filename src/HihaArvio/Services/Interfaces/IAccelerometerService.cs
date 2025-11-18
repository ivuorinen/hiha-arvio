using HihaArvio.Models;

namespace HihaArvio.Services.Interfaces;

/// <summary>
/// Platform abstraction for accelerometer sensor access.
/// Provides accelerometer readings that can come from:
/// - Real device accelerometer (iOS, Android)
/// - Mouse movement simulation (Desktop, Web)
/// - Keyboard shortcuts (Desktop)
/// </summary>
public interface IAccelerometerService
{
    /// <summary>
    /// Event raised when new accelerometer reading is available.
    /// Frequency depends on platform (typically 60-100Hz for real sensors).
    /// </summary>
    event EventHandler<SensorReading>? ReadingChanged;

    /// <summary>
    /// Starts monitoring accelerometer sensor.
    /// Begins raising ReadingChanged events.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops monitoring accelerometer sensor.
    /// Stops raising ReadingChanged events.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets whether accelerometer is supported on this platform/device.
    /// For simulated implementations, this should return true.
    /// </summary>
    bool IsSupported { get; }
}
