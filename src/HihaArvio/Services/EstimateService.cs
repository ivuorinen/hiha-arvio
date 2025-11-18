using System.Security.Cryptography;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Services;

/// <summary>
/// Service for generating time estimates based on shake data.
/// Implements two-pool selection algorithm per spec and easter egg logic.
/// Pools expanded 5x from specification for more variety.
/// </summary>
public class EstimateService : IEstimateService
{
    // Work Mode - Gentle Shake Pool (35 items - 5x spec's 7)
    // Conservative professional estimates
    private static readonly string[] WorkGentlePool =
    {
        "2 hours", "4 hours", "1 day", "2 days", "3 days", "5 days", "1 week",
        "3 hours", "6 hours", "1.5 days", "4 days", "6 days", "1.5 weeks",
        "2.5 hours", "5 hours", "1.5 hours", "3.5 days", "4.5 days", "2 weeks",
        "7 hours", "8 hours", "1.25 days", "2.5 days", "5.5 days", "10 days",
        "90 minutes", "150 minutes", "2.75 days", "3.25 days", "8 days", "12 days",
        "3.5 hours", "5.5 hours", "1.75 days", "2.25 days"
    };

    // Work Mode - Hard Shake Pool (60 items - 5x spec's 12)
    // Wide range professional estimates including optimistic and pessimistic
    private static readonly string[] WorkHardPool =
    {
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

    // Generic Mode - Gentle Shake Pool (40 items - 5x spec's 8)
    // Short, specific timeframes
    private static readonly string[] GenericGentlePool =
    {
        "1 minute", "5 minutes", "10 minutes", "15 minutes", "30 minutes",
        "1 hour", "2 hours", "3 hours",
        "2 minutes", "7 minutes", "12 minutes", "20 minutes", "45 minutes",
        "90 minutes", "2.5 hours", "4 hours",
        "3 minutes", "8 minutes", "18 minutes", "25 minutes", "40 minutes",
        "75 minutes", "3.5 hours", "5 hours",
        "4 minutes", "6 minutes", "9 minutes", "22 minutes", "35 minutes",
        "50 minutes", "4.5 hours", "6 hours",
        "11 minutes", "13 minutes", "16 minutes", "28 minutes", "55 minutes",
        "65 minutes", "5.5 hours", "7 hours"
    };

    // Generic Mode - Hard Shake Pool (75 items - 5x spec's 15)
    // Wider range from seconds to months
    private static readonly string[] GenericHardPool =
    {
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
        // Per spec: Easter egg - duration > 15 seconds forces Humorous mode
        if (duration > TimeSpan.FromSeconds(15))
        {
            mode = EstimateMode.Humorous;
        }

        // Select estimate using two-pool algorithm per spec
        string selectedEstimate;

        if (mode == EstimateMode.Humorous)
        {
            // Humorous mode uses single pool
            selectedEstimate = SelectRandomFromPool(HumorousPool);
        }
        else
        {
            // Work and Generic modes use two-pool selection based on intensity
            var (gentlePool, hardPool) = mode switch
            {
                EstimateMode.Work => (WorkGentlePool, WorkHardPool),
                EstimateMode.Generic => (GenericGentlePool, GenericHardPool),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid estimate mode")
            };

            // Per spec: Choose pool based on intensity threshold
            // Gentle shake (low intensity) uses gentle pool
            // Hard shake (high intensity) uses hard pool
            var pool = intensity < 0.5 ? gentlePool : hardPool;
            selectedEstimate = SelectRandomFromPool(pool);
        }

        // Create and return EstimateResult with all metadata
        return EstimateResult.Create(selectedEstimate, mode, intensity, duration);
    }

    /// <summary>
    /// Selects a random item from the pool using cryptographically secure RNG.
    /// Per spec: Must use cryptographically secure random number generation.
    /// </summary>
    private static string SelectRandomFromPool(string[] pool)
    {
        var index = RandomNumberGenerator.GetInt32(0, pool.Length);
        return pool[index];
    }
}
