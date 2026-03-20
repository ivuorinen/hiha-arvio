using System.Security.Cryptography;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Services;

/// <summary>
/// Service for generating time estimates based on shake data.
/// Implements range-based selection algorithm per spec §3.2.3 and easter egg logic.
/// Pools expanded 5x from specification for more variety, ordered from conservative to extreme.
/// </summary>
public class EstimateService : IEstimateService
{
    // Work Mode - Single ordered pool (95 items: 35 conservative + 60 wide-range)
    // Ordered from conservative professional estimates to wide-range including optimistic/pessimistic
    private static readonly string[] WorkPool =
    {
        // Conservative estimates (gentle range)
        "2 hours", "4 hours", "1 day", "2 days", "3 days", "5 days", "1 week",
        "3 hours", "6 hours", "1.5 days", "4 days", "6 days", "1.5 weeks",
        "2.5 hours", "5 hours", "1.5 hours", "3.5 days", "4.5 days", "2 weeks",
        "7 hours", "8 hours", "1.25 days", "2.5 days", "5.5 days", "10 days",
        "90 minutes", "150 minutes", "2.75 days", "3.25 days", "8 days", "12 days",
        "3.5 hours", "5.5 hours", "1.75 days", "2.25 days",
        // Wide-range estimates (hard range)
        "15 minutes", "30 minutes", "1 hour", "2 hours", "1 day", "3 days",
        "1 week", "2 weeks", "1 month", "3 months", "6 months", "1 year",
        "20 minutes", "45 minutes", "90 minutes", "3 hours", "2 days", "4 days",
        "10 days", "3 weeks", "6 weeks", "2 months", "4 months", "8 months",
        "25 minutes", "40 minutes", "75 minutes", "4 hours", "5 days", "1.5 weeks",
        "4 weeks", "5 weeks", "1.5 months", "2.5 months", "5 months", "9 months",
        "10 minutes", "35 minutes", "50 minutes", "2.5 hours", "6 days", "8 days",
        "9 days", "12 days", "5 days", "7 days", "1.25 months", "1.75 months",
        "3.5 months", "7 months", "10 months", "14 months", "18 months", "2 years",
        "55 minutes", "65 minutes", "100 minutes", "120 minutes", "11 days", "13 days"
    };

    // Generic Mode - Single ordered pool (115 items: 40 short + 75 wide-range)
    // Ordered from short specific timeframes to wider range from seconds to months
    private static readonly string[] GenericPool =
    {
        // Short, specific timeframes (gentle range)
        "1 minute", "5 minutes", "10 minutes", "15 minutes", "30 minutes",
        "1 hour", "2 hours", "3 hours",
        "2 minutes", "7 minutes", "12 minutes", "20 minutes", "45 minutes",
        "90 minutes", "2.5 hours", "4 hours",
        "3 minutes", "8 minutes", "18 minutes", "25 minutes", "40 minutes",
        "75 minutes", "3.5 hours", "5 hours",
        "4 minutes", "6 minutes", "9 minutes", "22 minutes", "35 minutes",
        "50 minutes", "4.5 hours", "6 hours",
        "11 minutes", "13 minutes", "16 minutes", "28 minutes", "55 minutes",
        "65 minutes", "5.5 hours", "7 hours",
        // Wider range from seconds to months (hard range)
        "30 seconds", "1 minute", "5 minutes", "15 minutes", "30 minutes",
        "1 hour", "2 hours", "6 hours", "12 hours", "1 day",
        "3 days", "1 week", "2 weeks", "1 month",
        "45 seconds", "2 minutes", "7 minutes", "20 minutes", "45 minutes",
        "90 minutes", "3 hours", "8 hours", "18 hours", "2 days",
        "4 days", "10 days", "3 weeks", "6 weeks", "2 months",
        "1 minute 30 seconds", "3 minutes", "10 minutes", "25 minutes", "50 minutes",
        "75 minutes", "4 hours", "9 hours", "15 hours", "1.5 days",
        "5 days", "8 days", "12 days", "4 weeks", "2.5 months",
        "20 seconds", "40 seconds", "4 minutes", "12 minutes", "35 minutes",
        "55 minutes", "5 hours", "7 hours", "10 hours", "20 hours",
        "2.5 days", "6 days", "9 days", "11 days", "5 weeks", "3 months",
        "15 seconds", "50 seconds", "6 minutes", "8 minutes", "14 minutes",
        "40 minutes", "100 minutes", "120 minutes", "11 hours", "16 hours",
        "22 hours", "3.5 days", "7 days", "14 days", "3.5 weeks"
    };

    // Humorous Mode - Single Pool (45 items - 5x spec's 9)
    // Comedic time estimates for easter egg
    private static readonly string[] HumorousPool =
    {
        "5 minutes", "tomorrow", "eventually", "next quarter",
        "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement",
        "when pigs fly", "next decade", "in another life", "ask again later",
        "two Tuesdays from now", "sometime this century", "after the heat death of the universe",
        "once you've learned Haskell", "when JavaScript makes sense", "next month maybe",
        "before the next ice age", "in a parallel universe", "when I feel like it",
        "after lunch", "probably never", "when the stars align", "in your dreams",
        "next sprint (we promise)", "when the backlog is empty", "after code review",
        "when tests pass", "when dependencies update themselves", "real soon now",
        "two weeks (famous last words)", "when management understands agile", "after the rewrite",
        "when the bugs fix themselves", "in production (maybe)", "after coffee",
        "when the wifi works", "next year for sure", "in the year 2525",
        "when documentation is up to date", "after we migrate to the cloud", "eventually (probably)",
        "when the build is green", "after the standup"
    };

    /// <inheritdoc/>
    public EstimateResult GenerateEstimate(double intensity, TimeSpan duration, EstimateMode mode)
    {
        // Per spec §3.2.3: Easter egg - duration > 15 seconds forces Humorous mode
        if (duration > TimeSpan.FromSeconds(15))
        {
            mode = EstimateMode.Humorous;
        }

        // Select estimate pool based on mode
        var pool = mode switch
        {
            EstimateMode.Work => WorkPool,
            EstimateMode.Generic => GenericPool,
            EstimateMode.Humorous => HumorousPool,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid estimate mode")
        };

        // Per spec §3.2.3: Use intensity to determine range within pool
        var rangeEnd = CalculateRangeEnd(pool.Length, intensity);
        var selectedEstimate = SelectRandomFromRange(pool, rangeEnd);

        // Create and return EstimateResult with all metadata
        return EstimateResult.Create(selectedEstimate, mode, intensity, duration);
    }

    /// <summary>
    /// Calculates the upper bound of the selectable range within a pool based on intensity.
    /// Per spec §3.2.3:
    ///   intensity &lt; 0.3 → first 20% of pool
    ///   intensity &lt; 0.7 → first 50% of pool
    ///   intensity &gt;= 0.7 → entire pool
    /// </summary>
    private static int CalculateRangeEnd(int poolLength, double intensity)
    {
        double fraction = intensity switch
        {
            < 0.3 => 0.2,
            < 0.7 => 0.5,
            _ => 1.0
        };

        return Math.Max(1, (int)Math.Ceiling(poolLength * fraction));
    }

    /// <summary>
    /// Selects a random item from the first <paramref name="rangeEnd"/> items of the pool
    /// using cryptographically secure RNG.
    /// </summary>
    private static string SelectRandomFromRange(string[] pool, int rangeEnd)
    {
        var index = RandomNumberGenerator.GetInt32(0, rangeEnd);
        return pool[index];
    }
}
