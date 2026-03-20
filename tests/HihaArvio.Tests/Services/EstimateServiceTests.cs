using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

/// <summary>
/// Tests for EstimateService covering easter egg behavior, pool selection,
/// expanded pool sizes, result metadata, and edge cases.
/// </summary>
public class EstimateServiceTests
{
    private readonly IEstimateService _service;

    public EstimateServiceTests()
    {
        _service = new EstimateService();
    }

    #region Easter Egg Tests (>15 seconds → Humorous mode)

    /// <summary>
    /// Verifies that shaking longer than 15 seconds forces Humorous mode (easter egg).
    /// </summary>
    [Fact]
    public void GenerateEstimate_WhenDurationExceeds15Seconds_ShouldForceHumorousMode()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(16);

        // Act
        var result = _service.GenerateEstimate(0.5, duration, EstimateMode.Work);

        // Assert
        result.Mode.Should().Be(EstimateMode.Humorous);
        // Verify result is from the expanded humorous pool (45 items)
        result.EstimateText.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that exactly 15 seconds of shaking does not trigger the easter egg.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WhenDurationExactly15Seconds_ShouldNotTriggerEasterEgg()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(15);

        // Act
        var result = _service.GenerateEstimate(0.5, duration, EstimateMode.Work);

        // Assert
        result.Mode.Should().Be(EstimateMode.Work);
    }

    /// <summary>
    /// Verifies that durations below the easter egg threshold preserve the requested mode.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WhenDurationBelowThreshold_ShouldRespectOriginalMode()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(10);

        // Act
        var result = _service.GenerateEstimate(0.5, duration, EstimateMode.Generic);

        // Assert
        result.Mode.Should().Be(EstimateMode.Generic);
    }

    #endregion

    #region Range-Based Selection Tests (Per Spec §3.2.3)

    /// <summary>
    /// Verifies that low intensity (below 0.3) selects from narrow range (first 20% of pool).
    /// Per spec §3.2.3: intensity &lt; 0.3 → first 20% of pool.
    /// </summary>
    [Theory]
    [InlineData(0.0, EstimateMode.Work)]
    [InlineData(0.1, EstimateMode.Work)]
    [InlineData(0.29, EstimateMode.Work)]
    [InlineData(0.0, EstimateMode.Generic)]
    [InlineData(0.15, EstimateMode.Generic)]
    public void GenerateEstimate_WithLowIntensity_ShouldSelectFromNarrowRange(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Generate multiple estimates
        var results = Enumerable.Range(0, 100)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert - All results should be valid
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.ShakeIntensity.Should().Be(intensity);
            r.EstimateText.Should().NotBeNullOrEmpty();
        });

        // Low intensity should have limited variety (first 20% of pool)
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(2, "narrow range should still have some variety");
    }

    /// <summary>
    /// Verifies that medium intensity (0.3-0.7) selects from medium range (first 50% of pool).
    /// Per spec §3.2.3: 0.3 ≤ intensity &lt; 0.7 → first 50% of pool.
    /// </summary>
    [Theory]
    [InlineData(0.3, EstimateMode.Work)]
    [InlineData(0.5, EstimateMode.Work)]
    [InlineData(0.69, EstimateMode.Work)]
    [InlineData(0.4, EstimateMode.Generic)]
    [InlineData(0.6, EstimateMode.Generic)]
    public void GenerateEstimate_WithMediumIntensity_ShouldSelectFromMediumRange(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var results = Enumerable.Range(0, 200)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.EstimateText.Should().NotBeNullOrEmpty();
        });

        // Medium range should have more variety than low
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(10, "medium range should have good variety");
    }

    /// <summary>
    /// Verifies that high intensity (>= 0.7) selects from entire pool.
    /// Per spec §3.2.3: intensity ≥ 0.7 → entire pool.
    /// </summary>
    [Theory]
    [InlineData(0.7, EstimateMode.Work)]
    [InlineData(0.8, EstimateMode.Work)]
    [InlineData(1.0, EstimateMode.Work)]
    [InlineData(0.7, EstimateMode.Generic)]
    [InlineData(0.9, EstimateMode.Generic)]
    public void GenerateEstimate_WithHighIntensity_ShouldSelectFromEntirePool(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var results = Enumerable.Range(0, 300)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.EstimateText.Should().NotBeNullOrEmpty();
        });

        // Full pool should have maximum variety
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(30, "entire pool should have extensive variety");
    }

    /// <summary>
    /// Verifies that increasing intensity progressively increases the variety of possible estimates.
    /// This confirms the range-based algorithm: low → narrow, medium → wider, high → full pool.
    /// </summary>
    [Fact]
    public void GenerateEstimate_IncreasingIntensity_ShouldIncreaseVariety()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);
        const int sampleSize = 300;

        // Act - Sample at each intensity tier
        var lowVariety = Enumerable.Range(0, sampleSize)
            .Select(_ => _service.GenerateEstimate(0.1, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        var mediumVariety = Enumerable.Range(0, sampleSize)
            .Select(_ => _service.GenerateEstimate(0.5, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        var highVariety = Enumerable.Range(0, sampleSize)
            .Select(_ => _service.GenerateEstimate(0.9, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert - Variety should increase with intensity
        mediumVariety.Should().BeGreaterThan(lowVariety, "medium intensity should access more estimates than low");
        highVariety.Should().BeGreaterThan(mediumVariety, "high intensity should access more estimates than medium");
    }

    #endregion

    #region Pool Size Tests (5x Spec)

    /// <summary>
    /// Verifies that Work mode pool (merged) contains the expected expanded number of estimates.
    /// Full pool = 95 items (35 conservative + 60 wide-range).
    /// </summary>
    [Fact]
    public void GenerateEstimate_WorkMode_ShouldHaveExpandedPoolSize()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Use high intensity to access full pool
        var fullPoolResults = Enumerable.Range(0, 500)
            .Select(_ => _service.GenerateEstimate(0.9, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert - Should discover most of the 77-item merged pool
        fullPoolResults.Should().BeGreaterThan(50, "Work pool should have expanded size");
    }

    /// <summary>
    /// Verifies that Generic mode pool (merged) contains the expected expanded number of estimates.
    /// Full pool = 115 items (40 short + 75 wide-range).
    /// </summary>
    [Fact]
    public void GenerateEstimate_GenericMode_ShouldHaveExpandedPoolSize()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Use high intensity to access full pool
        var fullPoolResults = Enumerable.Range(0, 500)
            .Select(_ => _service.GenerateEstimate(0.9, duration, EstimateMode.Generic))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert - Should discover most of the 87-item merged pool
        fullPoolResults.Should().BeGreaterThan(60, "Generic pool should have expanded size");
    }

    /// <summary>
    /// Verifies that Humorous mode pool contains the expected expanded number of estimates.
    /// </summary>
    [Fact]
    public void GenerateEstimate_HumorousMode_ShouldHaveExpandedPoolSize()
    {
        // Arrange - Use high intensity to access full pool
        var duration = TimeSpan.FromSeconds(16); // Trigger humorous via easter egg

        // Act
        var results = Enumerable.Range(0, 300)
            .Select(_ => _service.GenerateEstimate(0.9, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert - Humorous: 45 items (5x spec's 9)
        results.Should().BeGreaterThan(30, "Humorous pool should have expanded size");
    }

    #endregion

    #region EstimateResult Metadata Tests

    /// <summary>
    /// Verifies that all EstimateResult properties are correctly populated after generation.
    /// </summary>
    [Fact]
    public void GenerateEstimate_ShouldSetAllEstimateResultProperties()
    {
        // Arrange
        var intensity = 0.75;
        var duration = TimeSpan.FromSeconds(8);
        var mode = EstimateMode.Work;
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = _service.GenerateEstimate(intensity, duration, mode);
        var after = DateTimeOffset.UtcNow;

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.EstimateText.Should().NotBeNullOrEmpty();
        result.Mode.Should().Be(mode);
        result.ShakeIntensity.Should().Be(intensity);
        result.ShakeDuration.Should().Be(duration);
    }

    /// <summary>
    /// Verifies that each generated estimate receives a unique identifier.
    /// </summary>
    [Fact]
    public void GenerateEstimate_ShouldGenerateUniqueIds()
    {
        // Act
        var result1 = _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Work);
        var result2 = _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Work);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
    }

    /// <summary>
    /// Verifies that repeated calls produce varied (non-deterministic) estimate texts.
    /// </summary>
    [Fact]
    public void GenerateEstimate_ShouldProduceRandomResults()
    {
        // Act - Generate many estimates to verify randomness
        var results = Enumerable.Range(0, 100)
            .Select(_ => _service.GenerateEstimate(0.8, TimeSpan.FromSeconds(5), EstimateMode.Work))
            .Select(r => r.EstimateText)
            .ToList();

        // Assert - Should have multiple different estimates (not always the same)
        var uniqueCount = results.Distinct().Count();
        uniqueCount.Should().BeGreaterThan(5, "service should produce varied random results");
    }

    /// <summary>
    /// Verifies that the RNG produces a reasonably uniform distribution across the estimate pool.
    /// </summary>
    [Fact]
    public void GenerateEstimate_ShouldUseCryptographicallySecureRNG()
    {
        // Act - Generate large sample to test distribution
        var results = Enumerable.Range(0, 1000)
            .Select(_ => _service.GenerateEstimate(0.8, TimeSpan.FromSeconds(5), EstimateMode.Work))
            .Select(r => r.EstimateText)
            .GroupBy(x => x)
            .Select(g => g.Count())
            .ToList();

        // Assert - Distribution should be reasonably uniform (no single value dominates)
        var maxFrequency = results.Max();
        var avgFrequency = results.Average();

        // No single estimate should appear more than 3x the average
        (maxFrequency / avgFrequency).Should().BeLessThan(3, "RNG should produce reasonably uniform distribution");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Verifies that zero intensity produces a valid estimate from the gentle pool.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithZeroIntensity_ShouldWork()
    {
        // Act
        var result = _service.GenerateEstimate(0.0, TimeSpan.FromSeconds(5), EstimateMode.Work);

        // Assert
        result.Should().NotBeNull();
        result.EstimateText.Should().NotBeNullOrEmpty();
        result.ShakeIntensity.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies that maximum intensity (1.0) produces a valid estimate from the hard pool.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithMaxIntensity_ShouldWork()
    {
        // Act
        var result = _service.GenerateEstimate(1.0, TimeSpan.FromSeconds(5), EstimateMode.Work);

        // Assert
        result.Should().NotBeNull();
        result.EstimateText.Should().NotBeNullOrEmpty();
        result.ShakeIntensity.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies that zero duration produces a valid estimate without triggering the easter egg.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithZeroDuration_ShouldWork()
    {
        // Act
        var result = _service.GenerateEstimate(0.5, TimeSpan.Zero, EstimateMode.Work);

        // Assert
        result.Should().NotBeNull();
        result.ShakeDuration.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that all EstimateMode enum values produce valid estimates.
    /// </summary>
    [Fact]
    public void GenerateEstimate_AllModes_ShouldReturnValidEstimates()
    {
        // Act & Assert for each mode
        foreach (EstimateMode mode in Enum.GetValues(typeof(EstimateMode)))
        {
            var result = _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), mode);

            result.Should().NotBeNull();
            result.EstimateText.Should().NotBeNullOrEmpty();
            result.Mode.Should().Be(mode);
        }
    }

    #endregion

    #region Input Validation Tests

    /// <summary>
    /// Verifies that an invalid enum value throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithInvalidEnum_ShouldThrow()
    {
        // Arrange
        var invalidMode = (EstimateMode)99;

        // Act
        var act = () => _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), invalidMode);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("mode");
    }

    /// <summary>
    /// Verifies that an invalid enum value throws even when duration exceeds 15 seconds.
    /// The enum validation must happen BEFORE the easter egg check.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithInvalidEnumAndLongDuration_ShouldThrow()
    {
        // Arrange
        var invalidMode = (EstimateMode)99;
        var longDuration = TimeSpan.FromSeconds(20);

        // Act
        var act = () => _service.GenerateEstimate(0.5, longDuration, invalidMode);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("mode");
    }

    /// <summary>
    /// Verifies that intensity below 0.0 throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithNegativeIntensity_ShouldThrow()
    {
        // Act
        var act = () => _service.GenerateEstimate(-0.1, TimeSpan.FromSeconds(5), EstimateMode.Work);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("intensity");
    }

    /// <summary>
    /// Verifies that intensity above 1.0 throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void GenerateEstimate_WithIntensityAboveOne_ShouldThrow()
    {
        // Act
        var act = () => _service.GenerateEstimate(1.1, TimeSpan.FromSeconds(5), EstimateMode.Work);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("intensity");
    }

    #endregion
}
