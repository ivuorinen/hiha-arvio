using HihaArvio.Models;

namespace HihaArvio.Services.Interfaces;

/// <summary>
/// Service for generating time estimates based on shake data.
/// </summary>
public interface IEstimateService
{
    /// <summary>
    /// Generates a time estimate based on shake intensity, duration, and selected mode.
    /// </summary>
    /// <param name="intensity">Normalized shake intensity (0.0 to 1.0).</param>
    /// <param name="duration">Duration of the shake gesture.</param>
    /// <param name="mode">The estimation mode (Work, Generic, or Humorous).</param>
    /// <returns>An EstimateResult with the generated estimate and metadata.</returns>
    /// <remarks>
    /// Per spec: If duration exceeds 15 seconds, mode is automatically changed to Humorous (easter egg).
    /// Intensity determines the range of possible estimates:
    /// - Low (0.0-0.3): narrow range (first 20% of pool)
    /// - Medium (0.3-0.7): medium range (first 50% of pool)
    /// - High (0.7-1.0): full range (entire pool)
    /// </remarks>
    EstimateResult GenerateEstimate(double intensity, TimeSpan duration, EstimateMode mode);
}
