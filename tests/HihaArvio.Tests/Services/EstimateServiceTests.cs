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
        result.EstimateText.Should().BeOneOf(
            "5 minutes", "tomorrow", "eventually", "next quarter",
            "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement");
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

    #region Intensity-Based Range Selection Tests

    [Theory]
    [InlineData(0.0, EstimateMode.Work)]  // Lowest intensity
    [InlineData(0.1, EstimateMode.Work)]
    [InlineData(0.29, EstimateMode.Work)]
    [InlineData(0.0, EstimateMode.Generic)]
    [InlineData(0.2, EstimateMode.Generic)]
    public void GenerateEstimate_WithLowIntensity_ShouldReturnFromNarrowRange(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act - Generate multiple estimates to test range
        var results = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert - All results should be from the narrow range (first 20% of pool)
        // We can't test exact values without knowing implementation, but we can verify consistency
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.ShakeIntensity.Should().Be(intensity);
            r.EstimateText.Should().NotBeNullOrEmpty();
        });

        // The variety should be limited (narrow range)
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeLessThan(10, "low intensity should produce limited variety");
    }

    [Theory]
    [InlineData(0.3, EstimateMode.Work)]
    [InlineData(0.5, EstimateMode.Work)]
    [InlineData(0.69, EstimateMode.Work)]
    [InlineData(0.4, EstimateMode.Generic)]
    public void GenerateEstimate_WithMediumIntensity_ShouldReturnFromMediumRange(double intensity, EstimateMode mode)
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var results = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(intensity, duration, mode))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Mode.Should().Be(mode);
            r.ShakeIntensity.Should().Be(intensity);
        });

        // Medium range should have more variety than low
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(2, "medium intensity should produce moderate variety");
    }

    [Theory]
    [InlineData(0.7, EstimateMode.Work)]
    [InlineData(0.85, EstimateMode.Work)]
    [InlineData(1.0, EstimateMode.Work)]
    [InlineData(0.9, EstimateMode.Generic)]
    public void GenerateEstimate_WithHighIntensity_ShouldReturnFromFullRange(double intensity, EstimateMode mode)
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
        });

        // High intensity should have maximum variety
        var uniqueEstimates = results.Select(r => r.EstimateText).Distinct().Count();
        uniqueEstimates.Should().BeGreaterThan(5, "high intensity should produce maximum variety");
    }

    #endregion

    #region Mode-Specific Estimate Pool Tests

    [Fact]
    public void GenerateEstimate_InWorkMode_ShouldReturnWorkEstimates()
    {
        // Arrange
        var validWorkEstimates = new[]
        {
            "2 hours", "4 hours", "1 day", "2 days", "3 days", "5 days", "1 week",
            "15 minutes", "30 minutes", "1 hour", "2 weeks", "1 month", "3 months", "6 months", "1 year"
        };

        // Act
        var results = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(0.8, TimeSpan.FromSeconds(5), EstimateMode.Work))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.EstimateText.Should().BeOneOf(validWorkEstimates);
            r.Mode.Should().Be(EstimateMode.Work);
        });
    }

    [Fact]
    public void GenerateEstimate_InGenericMode_ShouldReturnGenericEstimates()
    {
        // Arrange
        var validGenericEstimates = new[]
        {
            "1 minute", "5 minutes", "10 minutes", "15 minutes", "30 minutes",
            "1 hour", "2 hours", "3 hours", "6 hours", "12 hours",
            "1 day", "3 days", "1 week", "2 weeks", "1 month", "30 seconds"
        };

        // Act
        var results = Enumerable.Range(0, 50)
            .Select(_ => _service.GenerateEstimate(0.8, TimeSpan.FromSeconds(5), EstimateMode.Generic))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.EstimateText.Should().BeOneOf(validGenericEstimates);
            r.Mode.Should().Be(EstimateMode.Generic);
        });
    }

    [Fact]
    public void GenerateEstimate_InHumorousMode_ShouldReturnHumorousEstimates()
    {
        // Arrange
        var validHumorousEstimates = new[]
        {
            "5 minutes", "tomorrow", "eventually", "next quarter",
            "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement"
        };

        // Act
        var results = Enumerable.Range(0, 30)
            .Select(_ => _service.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Humorous))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.EstimateText.Should().BeOneOf(validHumorousEstimates);
            r.Mode.Should().Be(EstimateMode.Humorous);
        });
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
        uniqueCount.Should().BeGreaterThan(1, "service should produce varied random results");
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

    #endregion
}
