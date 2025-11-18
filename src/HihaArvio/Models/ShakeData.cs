namespace HihaArvio.Models;

/// <summary>
/// Represents current shake gesture data.
/// </summary>
public class ShakeData
{
    private double _intensity;

    /// <summary>
    /// Gets or sets the normalized shake intensity (0.0 to 1.0).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside [0.0, 1.0] range.</exception>
    public double Intensity
    {
        get => _intensity;
        set
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Intensity must be between 0.0 and 1.0.");
            }

            _intensity = value;
        }
    }

    /// <summary>
    /// Gets or sets the duration of the current shake gesture.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a shake is currently in progress.
    /// </summary>
    public bool IsShaking { get; set; }
}
