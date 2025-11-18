namespace HihaArvio.Models;

/// <summary>
/// Represents a time estimate result generated from a shake gesture.
/// </summary>
public class EstimateResult
{
    private string _estimateText = string.Empty;
    private double _shakeIntensity;

    /// <summary>
    /// Gets or sets the unique identifier for this estimate.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this estimate was generated.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the estimate text (e.g., "2 weeks", "eventually").
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or whitespace.</exception>
    public string EstimateText
    {
        get => _estimateText;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "EstimateText cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("EstimateText cannot be empty or whitespace.", nameof(value));
            }

            _estimateText = value;
        }
    }

    /// <summary>
    /// Gets or sets the estimation mode (Work, Generic, or Humorous).
    /// </summary>
    public EstimateMode Mode { get; set; }

    /// <summary>
    /// Gets or sets the normalized shake intensity (0.0 to 1.0).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside [0.0, 1.0] range.</exception>
    public double ShakeIntensity
    {
        get => _shakeIntensity;
        set
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "ShakeIntensity must be between 0.0 and 1.0.");
            }

            _shakeIntensity = value;
        }
    }

    /// <summary>
    /// Gets or sets the duration of the shake gesture.
    /// </summary>
    public TimeSpan ShakeDuration { get; set; }

    /// <summary>
    /// Creates a new EstimateResult with the specified values and auto-generated ID and timestamp.
    /// </summary>
    /// <param name="estimateText">The estimate text.</param>
    /// <param name="mode">The estimation mode.</param>
    /// <param name="shakeIntensity">The shake intensity (0.0 to 1.0).</param>
    /// <param name="shakeDuration">The shake duration.</param>
    /// <returns>A new EstimateResult instance.</returns>
    public static EstimateResult Create(
        string estimateText,
        EstimateMode mode,
        double shakeIntensity,
        TimeSpan shakeDuration)
    {
        return new EstimateResult
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EstimateText = estimateText,
            Mode = mode,
            ShakeIntensity = shakeIntensity,
            ShakeDuration = shakeDuration
        };
    }
}
