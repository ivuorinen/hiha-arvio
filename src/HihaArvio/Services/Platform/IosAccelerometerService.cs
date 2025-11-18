using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Services;

/// <summary>
/// iOS implementation of IAccelerometerService using MAUI's built-in accelerometer API.
/// Works on iOS devices and simulator (simulator provides simulated data).
/// </summary>
public class IosAccelerometerService : IAccelerometerService
{
#pragma warning disable CS0649 // Field is never assigned (conditional compilation)
    private bool _isMonitoring;
#pragma warning restore CS0649

    /// <inheritdoc/>
#pragma warning disable CS0067 // Event is never used (conditional compilation)
    public event EventHandler<SensorReading>? ReadingChanged;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public bool IsSupported =>
#if IOS || MACCATALYST
        Microsoft.Maui.Devices.Sensors.Accelerometer.Default.IsSupported;
#else
        false;
#endif

    /// <inheritdoc/>
    public void Start()
    {
        if (_isMonitoring)
        {
            return; // Already monitoring
        }

#if IOS || MACCATALYST
        try
        {
            // Start monitoring at default speed (UI refresh rate)
            Microsoft.Maui.Devices.Sensors.Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
            Microsoft.Maui.Devices.Sensors.Accelerometer.Default.Start(Microsoft.Maui.Devices.Sensors.SensorSpeed.UI);
            _isMonitoring = true;
        }
        catch (Exception)
        {
            // Accelerometer not supported or permission denied
            // Fail silently - IsSupported will be false
        }
#endif
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_isMonitoring)
        {
            return; // Already stopped
        }

#if IOS || MACCATALYST
        try
        {
            Microsoft.Maui.Devices.Sensors.Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
            Microsoft.Maui.Devices.Sensors.Accelerometer.Default.Stop();
            _isMonitoring = false;
        }
        catch (Exception)
        {
            // Fail silently
        }
#endif
    }

#if IOS || MACCATALYST
    private void OnAccelerometerReadingChanged(object? sender, Microsoft.Maui.Devices.Sensors.AccelerometerChangedEventArgs e)
    {
        var reading = e.Reading;

        // Convert MAUI AccelerometerData to our platform-agnostic SensorReading
        var sensorReading = new SensorReading
        {
            X = reading.Acceleration.X,
            Y = reading.Acceleration.Y,
            Z = reading.Acceleration.Z
        };

        ReadingChanged?.Invoke(this, sensorReading);
    }
#endif
}
