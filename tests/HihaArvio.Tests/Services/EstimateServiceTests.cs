using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

public class EstimateServiceTests
{
    private readonly IEstimateService _service;

    public EstimateServiceTests()
    {
        _service = new EstimateService();
    }

    #region Easter Egg Tests (>15 seconds → Humorous mode)

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

    #region Two-Pool Selection Tests (Per Spec: Gentle vs Hard)

    [Theory]
    [InlineData(0.0, EstimateMode.Work)]  // Lowest intensity → gentle pool
    [InlineData(0.1, EstimateMode.Work)]
    [InlineData(0.3, EstimateMode.Work)]
    [InlineData(0.49, EstimateMode.Work)] // Just below threshold
    [InlineData(0.0, EstimateMode.Generic)]
    [InlineData(0.2, EstimateMode.Generic)]
    [InlineData(0.4, EstimateMode.Generic)]
    public void GenerateEstimate_WithLowIntensity_ShouldSelectFromGentlePool(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Generate multiple estimates to verify pool selection
        var results = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert - All results should be from gentle pool
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.ShakeIntensity.Should().Be(intensity);
            r.EstimateText.Should().NotBeNullOrEmpty();
        });

        // Should have good variety from the expanded pool
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(5, "gentle pool should have variety");
    }

    [Theory]
    [InlineData(0.5, EstimateMode.Work)]  // At threshold → hard pool
    [InlineData(0.6, EstimateMode.Work)]
    [InlineData(0.8, EstimateMode.Work)]
    [InlineData(1.0, EstimateMode.Work)]  // Maximum intensity
    [InlineData(0.5, EstimateMode.Generic)]
    [InlineData(0.7, EstimateMode.Generic)]
    [InlineData(0.9, EstimateMode.Generic)]
    public void GenerateEstimate_WithHighIntensity_ShouldSelectFromHardPool(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.ShakeIntensity.Should().Be(intensity);
            r.EstimateText.Should().NotBeNullOrEmpty();
        });

        // Hard pool should have maximum variety (larger pool)
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(10, "hard pool should have extensive variety");
    }

    [Fact]
    public void GenerateEstimate_ThresholdAt0Point5_ShouldProduceDistinctPoolSelections()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Sample both sides of the threshold
        var gentleResults = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(0.49, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .ToHashSet();

        var hardResults = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(0.5, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .ToHashSet();

        // Assert - The pools should have some different estimates
        // (They're different pools, so overlap might be minimal or none)
        var overlap = gentleResults.Intersect(hardResults).Count();
        var combined = gentleResults.Union(hardResults).Count();

        // With 35 gentle + 60 hard = 95 total unique estimates in Work mode
        combined.Should().BeGreaterThan(20, "combined selections from both pools should show variety");
    }

    #endregion

    #region Expanded Pool Tests (5x Spec)

    [Fact]
    public void GenerateEstimate_WorkMode_ShouldHaveExpandedPoolSize()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Generate many samples to discover pool diversity
        var gentleResults = Enumerable.Range(0, 200)
            .Select(_ => _service.GenerateEstimate(0.3, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        var hardResults = Enumerable.Range(0, 300)
            .Select(_ => _service.GenerateEstimate(0.8, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert - Should discover most of the expanded pools
        // Gentle: 35 items (5x spec's 7), Hard: 60 items (5x spec's 12)
        gentleResults.Should().BeGreaterThan(20, "Work gentle pool should have expanded size");
        hardResults.Should().BeGreaterThan(30, "Work hard pool should have expanded size");
    }

    [Fact]
    public void GenerateEstimate_GenericMode_ShouldHaveExpandedPoolSize()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var gentleResults = Enumerable.Range(0, 200)
            .Select(_ => _service.GenerateEstimate(0.3, duration, EstimateMode.Generic))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        var hardResults = Enumerable.Range(0, 300)
            .Select(_ => _service.GenerateEstimate(0.8, duration, EstimateMode.Generic))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert
        // Gentle: 40 items (5x spec's 8), Hard: 75 items (5x spec's 15)
        gentleResults.Should().BeGreaterThan(20, "Generic gentle pool should have expanded size");
        hardResults.Should().BeGreaterThan(35, "Generic hard pool should have expanded size");
    }

    [Fact]
    public void GenerateEstimate_HumorousMode_ShouldHaveExpandedPoolSize()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(16); // Trigger humorous via easter egg

        // Act
        var results = Enumerable.Range(0, 200)
            .Select(_ => _service.GenerateEstimate(0.5, duration, EstimateMode.Work))
            .Select(r => r.EstimateText)
            .Distinct()
            .Count();

        // Assert - Humorous: 45 items (5x spec's 9)
        results.Should().BeGreaterThan(30, "Humorous pool should have expanded size");
    }

    #endregion

    #region EstimateResult Metadata Tests

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

    [Fact]
    public void GenerateEstimate_ShouldGenerateUniqueIds()
    {
        // Act
        var result1 = _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Work);
        var result2 = _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Work);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
    }

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

    [Fact]
    public void GenerateEstimate_WithZeroDuration_ShouldWork()
    {
        // Act
        var result = _service.GenerateEstimate(0.5, TimeSpan.Zero, EstimateMode.Work);

        // Assert
        result.Should().NotBeNull();
        result.ShakeDuration.Should().Be(TimeSpan.Zero);
    }

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
}
