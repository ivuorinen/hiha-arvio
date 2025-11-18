namespace HihaArvio.Models;

/// <summary>
/// Represents application settings and user preferences.
/// </summary>
public class AppSettings
{
    private int _maxHistorySize = 10;

    /// <summary>
    /// Gets or sets the selected estimate mode.
    /// Default is <see cref="EstimateMode.Work"/>.
    /// </summary>
    public EstimateMode SelectedMode { get; set; } = EstimateMode.Work;

    /// <summary>
    /// Gets or sets the maximum number of estimate results to keep in history.
    /// Default is 10.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0.</exception>
    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "MaxHistorySize must be greater than 0.");
            }

            _maxHistorySize = value;
        }
    }
}
