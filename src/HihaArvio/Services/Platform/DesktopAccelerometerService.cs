using HihaArvio.Models;
using HihaArvio.Services.Interfaces;
using System.Timers;

namespace HihaArvio.Services;

/// <summary>
/// Desktop implementation of IAccelerometerService using simulated accelerometer data.
/// Generates periodic readings with small variations to simulate device at rest.
/// In future, can be enhanced to track mouse movement for shake simulation.
/// </summary>
public class DesktopAccelerometerService : IAccelerometerService
{
    private System.Timers.Timer? _timer;
    private readonly Random _random;
    private const double BaseGravity = 1.0; // Device at rest experiences 1g

    public DesktopAccelerometerService()
    {
        _random = new Random();
    }

    /// <inheritdoc/>
    public event EventHandler<SensorReading>? ReadingChanged;

    /// <inheritdoc/>
    public bool IsSupported => true; // Always supported (simulated)

    /// <inheritdoc/>
    public void Start()
    {
        if (_timer != null)
        {
            return; // Already monitoring
        }

        // Generate readings at ~60Hz (UI refresh rate simulation)
        _timer = new System.Timers.Timer(16); // ~60 FPS
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (_timer == null)
        {
            return; // Already stopped
        }

        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
        _timer = null;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Generate simulated accelerometer reading
        // Device at rest: X≈0, Y≈0, Z≈1 (gravity pointing down)
        // Add small random noise to simulate realistic sensor data

        var reading = new SensorReading
        {
            X = GenerateNoiseValue(0.0, 0.05),
            Y = GenerateNoiseValue(0.0, 0.05),
            Z = GenerateNoiseValue(BaseGravity, 0.05)
        };

        ReadingChanged?.Invoke(this, reading);
    }

    private double GenerateNoiseValue(double center, double variation)
    {
        // Generate value with normal distribution around center
        return center + ((_random.NextDouble() - 0.5) * 2 * variation);
    }

    /// <summary>
    /// Simulates a shake gesture by generating high-intensity readings.
    /// Can be called from keyboard shortcut or test code.
    /// </summary>
    /// <param name="intensity">Shake intensity multiplier (1.0 = moderate shake).</param>
    public void SimulateShake(double intensity = 1.0)
    {
        if (_timer == null)
        {
            return; // Not monitoring
        }

        // Generate a burst of high-intensity readings
        for (int i = 0; i < 5; i++)
        {
            var reading = new SensorReading
            {
                X = (_random.NextDouble() - 0.5) * 3.0 * intensity,
                Y = (_random.NextDouble() - 0.5) * 3.0 * intensity,
                Z = BaseGravity + (_random.NextDouble() - 0.5) * 3.0 * intensity
            };

            ReadingChanged?.Invoke(this, reading);
        }
    }
}
