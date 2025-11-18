using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Services;

/// <summary>
/// Service for detecting shake gestures from accelerometer data.
/// Implements shake detection algorithm per spec with 1.5g threshold and 4g max intensity.
/// </summary>
public class ShakeDetectionService : IShakeDetectionService
{
    // Per spec: 1.5g threshold for shake detection
    private const double ShakeThresholdG = 1.5;

    // Per spec: 4g is maximum expected shake intensity for normalization
    private const double MaxShakeIntensityG = 4.0;

    // Gravity constant (at rest, device experiences 1g)
    private const double GravityG = 1.0;

    private ShakeData _currentShakeData;
    private bool _isMonitoring;
    private DateTimeOffset _shakeStartTime;
    private bool _wasShakingLastUpdate;

    public ShakeDetectionService()
    {
        _currentShakeData = new ShakeData
        {
            IsShaking = false,
            Intensity = 0.0,
            Duration = TimeSpan.Zero
        };
        _isMonitoring = false;
        _wasShakingLastUpdate = false;
    }

    /// <inheritdoc/>
    public ShakeData CurrentShakeData => _currentShakeData;

    /// <inheritdoc/>
    public bool IsMonitoring => _isMonitoring;

    /// <inheritdoc/>
    public event EventHandler<ShakeData>? ShakeDataChanged;

    /// <inheritdoc/>
    public void StartMonitoring()
    {
        _isMonitoring = true;
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        _isMonitoring = false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _currentShakeData = new ShakeData
        {
            IsShaking = false,
            Intensity = 0.0,
            Duration = TimeSpan.Zero
        };
        _wasShakingLastUpdate = false;
    }

    /// <inheritdoc/>
    public void ProcessAccelerometerReading(double x, double y, double z)
    {
        if (!_isMonitoring)
        {
            return;
        }

        // Calculate magnitude of acceleration vector
        var magnitude = Math.Sqrt(x * x + y * y + z * z);

        // Subtract gravity to get shake acceleration (device at rest = 1g)
        var shakeAcceleration = Math.Max(0, magnitude - GravityG);

        // Determine if shaking based on threshold
        var isShaking = shakeAcceleration >= ShakeThresholdG;

        // Normalize intensity to 0.0-1.0 range based on max expected shake
        var normalizedIntensity = isShaking
            ? Math.Min(1.0, shakeAcceleration / MaxShakeIntensityG)
            : 0.0;

        // Track shake duration
        TimeSpan duration;
        if (isShaking)
        {
            if (!_wasShakingLastUpdate)
            {
                // Shake just started - reset start time
                _shakeStartTime = DateTimeOffset.UtcNow;
                duration = TimeSpan.Zero;
            }
            else
            {
                // Shake continuing - calculate duration
                duration = DateTimeOffset.UtcNow - _shakeStartTime;
            }
        }
        else
        {
            // Not shaking - reset duration
            duration = TimeSpan.Zero;
        }

        // Check if state changed
        var stateChanged = isShaking != _wasShakingLastUpdate ||
                          Math.Abs(normalizedIntensity - _currentShakeData.Intensity) > 0.01 ||
                          duration != _currentShakeData.Duration;

        // Update current state
        _currentShakeData = new ShakeData
        {
            IsShaking = isShaking,
            Intensity = normalizedIntensity,
            Duration = duration
        };

        _wasShakingLastUpdate = isShaking;

        // Fire event if state changed
        if (stateChanged)
        {
            ShakeDataChanged?.Invoke(this, _currentShakeData);
        }
    }
}
