namespace HihaArvio.Models;

/// <summary>
/// Platform-agnostic representation of accelerometer sensor reading.
/// Values are in g-force units (1g = Earth's gravity = 9.8 m/s²).
/// Renamed to SensorReading to avoid conflict with Microsoft.Maui.Devices.Sensors.AccelerometerData.
/// </summary>
public record SensorReading
{
    /// <summary>
    /// Acceleration along the X-axis in g's.
    /// </summary>
    public required double X { get; init; }

    /// <summary>
    /// Acceleration along the Y-axis in g's.
    /// </summary>
    public required double Y { get; init; }

    /// <summary>
    /// Acceleration along the Z-axis in g's.
    /// </summary>
    public required double Z { get; init; }
}
