using HihaArvio.Models;

namespace HihaArvio.Services.Interfaces;

/// <summary>
/// Service for detecting shake gestures from accelerometer data.
/// Implements shake detection algorithm with intensity calculation.
/// </summary>
public interface IShakeDetectionService
{
    /// <summary>
    /// Gets the current shake data (intensity, duration, isShaking status).
    /// </summary>
    ShakeData CurrentShakeData { get; }

    /// <summary>
    /// Starts monitoring for shake gestures.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring for shake gestures.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Processes accelerometer reading and updates shake detection state.
    /// </summary>
    /// <param name="x">X-axis acceleration in g's.</param>
    /// <param name="y">Y-axis acceleration in g's.</param>
    /// <param name="z">Z-axis acceleration in g's.</param>
    void ProcessAccelerometerReading(double x, double y, double z);

    /// <summary>
    /// Resets the shake detection state (useful after generating an estimate).
    /// </summary>
    void Reset();

    /// <summary>
    /// Event raised when shake data changes (intensity, duration, or isShaking status).
    /// </summary>
    event EventHandler<ShakeData>? ShakeDataChanged;
}
