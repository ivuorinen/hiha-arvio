using System.Security.Cryptography;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Services;

/// <summary>
/// Service for generating time estimates based on shake data.
/// Implements intensity-based range selection and easter egg logic.
/// </summary>
public class EstimateService : IEstimateService
{
    // Per spec: Work mode estimates (wider ranges)
    private static readonly string[] WorkEstimates =
    {
        // Gentle shake range (first 20%)
        "2 hours", "4 hours",
        // Medium range (first 50%)
        "1 day", "2 days", "3 days", "5 days", "1 week",
        // Full range (entire pool)
        "15 minutes", "30 minutes", "1 hour", "2 weeks", "1 month", "3 months", "6 months", "1 year"
    };

    // Per spec: Generic mode estimates (wider ranges)
    private static readonly string[] GenericEstimates =
    {
        // Gentle shake range (first 20%)
        "1 minute", "5 minutes", "10 minutes",
        // Medium range (first 50%)
        "15 minutes", "30 minutes", "1 hour", "2 hours", "3 hours",
        // Full range (entire pool)
        "30 seconds", "6 hours", "12 hours", "1 day", "3 days", "1 week", "2 weeks", "1 month"
    };

    // Per spec: Humorous mode estimates (easter egg)
    private static readonly string[] HumorousEstimates =
    {
        "5 minutes", "tomorrow", "eventually", "next quarter",
        "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement"
    };

    /// <inheritdoc/>
    public EstimateResult GenerateEstimate(double intensity, TimeSpan duration, EstimateMode mode)
    {
        // Per spec: Easter egg - duration > 15 seconds forces Humorous mode
        if (duration > TimeSpan.FromSeconds(15))
        {
            mode = EstimateMode.Humorous;
        }

        // Select estimate pool based on mode
        var pool = mode switch
        {
            EstimateMode.Work => WorkEstimates,
            EstimateMode.Generic => GenericEstimates,
            EstimateMode.Humorous => HumorousEstimates,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid estimate mode")
        };

        // Calculate range based on intensity (per spec)
        var rangeSize = intensity switch
        {
            < 0.3 => (int)Math.Ceiling(pool.Length * 0.2),  // First 20% (narrow range)
            < 0.7 => (int)Math.Ceiling(pool.Length * 0.5),  // First 50% (medium range)
            _ => pool.Length                                 // Entire pool (full range)
        };

        // Ensure at least one item in range
        rangeSize = Math.Max(1, rangeSize);

        // Select random estimate from calculated range using cryptographically secure RNG (per spec)
        var selectedEstimate = SelectRandomFromRange(pool, rangeSize);

        // Create and return EstimateResult with all metadata
        return EstimateResult.Create(selectedEstimate, mode, intensity, duration);
    }

    /// <summary>
    /// Selects a random item from the first N items of the array using cryptographically secure RNG.
    /// </summary>
    private static string SelectRandomFromRange(string[] array, int rangeSize)
    {
        var index = RandomNumberGenerator.GetInt32(0, rangeSize);
        return array[index];
    }
}
