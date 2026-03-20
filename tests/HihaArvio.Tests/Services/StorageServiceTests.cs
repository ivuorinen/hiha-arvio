using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Services;

/// <summary>
/// Tests for the StorageService covering settings persistence, estimate history CRUD operations, auto-pruning, and edge cases.
/// </summary>
public class StorageServiceTests : IDisposable
{
    private readonly IStorageService _service;
    private readonly string _testDbPath;

    public StorageServiceTests()
    {
        // Use unique temp file for each test to ensure isolation
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_hiha_{Guid.NewGuid()}.db");
        _service = new StorageService(_testDbPath);
    }

    public void Dispose()
    {
        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    #region Settings Tests

    /// <summary>
    /// Verifies that default settings are returned when no settings have been saved.
    /// </summary>
    [Fact]
    public async Task LoadSettingsAsync_WhenNoSettingsExist_ShouldReturnDefaultSettings()
    {
        // Act
        var settings = await _service.LoadSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.SelectedMode.Should().Be(EstimateMode.Work);
        settings.MaxHistorySize.Should().Be(10);
    }

    /// <summary>
    /// Verifies that saved settings can be loaded back correctly.
    /// </summary>
    [Fact]
    public async Task SaveAndLoadSettings_ShouldPersistSettings()
    {
        // Arrange
        var settingsToSave = new AppSettings
        {
            SelectedMode = EstimateMode.Generic,
            MaxHistorySize = 20
        };

        // Act
        await _service.SaveSettingsAsync(settingsToSave);
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        loadedSettings.SelectedMode.Should().Be(EstimateMode.Generic);
        loadedSettings.MaxHistorySize.Should().Be(20);
    }

    /// <summary>
    /// Verifies that saving settings multiple times overwrites previous values.
    /// </summary>
    [Fact]
    public async Task SaveSettings_MultipleTimes_ShouldOverwritePreviousSettings()
    {
        // Arrange
        var settings1 = new AppSettings { SelectedMode = EstimateMode.Work, MaxHistorySize = 10 };
        var settings2 = new AppSettings { SelectedMode = EstimateMode.Humorous, MaxHistorySize = 50 };

        // Act
        await _service.SaveSettingsAsync(settings1);
        await _service.SaveSettingsAsync(settings2);
        var loaded = await _service.LoadSettingsAsync();

        // Assert
        loaded.SelectedMode.Should().Be(EstimateMode.Humorous);
        loaded.MaxHistorySize.Should().Be(50);
    }

    #endregion

    #region Estimate History Tests

    /// <summary>
    /// Verifies that a saved estimate can be retrieved from history.
    /// </summary>
    [Fact]
    public async Task SaveEstimateAsync_ShouldPersistEstimate()
    {
        // Arrange
        var estimate = EstimateResult.Create("2 weeks", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));

        // Act
        await _service.SaveEstimateAsync(estimate);
        var history = await _service.GetHistoryAsync();

        // Assert
        history.Should().ContainSingle();
        var saved = history.First();
        saved.Id.Should().Be(estimate.Id);
        saved.EstimateText.Should().Be("2 weeks");
        saved.Mode.Should().Be(EstimateMode.Work);
        saved.ShakeIntensity.Should().Be(0.5);
        saved.ShakeDuration.Should().Be(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that an empty history returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Act
        var history = await _service.GetHistoryAsync();

        // Assert
        history.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that history is returned in reverse chronological order.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_ShouldReturnNewestFirst()
    {
        // Arrange
        var estimate1 = EstimateResult.Create("1 day", EstimateMode.Work, 0.3, TimeSpan.FromSeconds(3));
        await Task.Delay(10); // Ensure different timestamps
        var estimate2 = EstimateResult.Create("2 days", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        await Task.Delay(10);
        var estimate3 = EstimateResult.Create("3 days", EstimateMode.Work, 0.7, TimeSpan.FromSeconds(7));

        // Act
        await _service.SaveEstimateAsync(estimate1);
        await _service.SaveEstimateAsync(estimate2);
        await _service.SaveEstimateAsync(estimate3);
        var history = await _service.GetHistoryAsync();

        // Assert
        history.Should().HaveCount(3);
        history[0].EstimateText.Should().Be("3 days"); // Newest
        history[1].EstimateText.Should().Be("2 days");
        history[2].EstimateText.Should().Be("1 day");  // Oldest
    }

    /// <summary>
    /// Verifies that the count parameter limits the number of returned results.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_WithCount_ShouldLimitResults()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            var estimate = EstimateResult.Create($"{i} days", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
            await _service.SaveEstimateAsync(estimate);
            await Task.Delay(5); // Ensure different timestamps
        }

        // Act
        var history = await _service.GetHistoryAsync(5);

        // Assert
        history.Should().HaveCount(5);
    }

    /// <summary>
    /// Verifies that history is automatically pruned when it exceeds MaxHistorySize.
    /// </summary>
    [Fact]
    public async Task SaveEstimateAsync_ShouldAutoPruneWhenExceedingMaxSize()
    {
        // Arrange
        var settings = new AppSettings { MaxHistorySize = 5 };
        await _service.SaveSettingsAsync(settings);

        // Act - Save 7 estimates (exceeds max of 5)
        for (int i = 0; i < 7; i++)
        {
            var estimate = EstimateResult.Create($"{i} days", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
            await _service.SaveEstimateAsync(estimate);
            await Task.Delay(5);
        }

        var count = await _service.GetHistoryCountAsync();

        // Assert - Should have pruned to max size
        count.Should().Be(5);
    }

    /// <summary>
    /// Verifies that auto-pruning retains the most recent estimates.
    /// </summary>
    [Fact]
    public async Task SaveEstimateAsync_AfterPruning_ShouldKeepNewestEstimates()
    {
        // Arrange
        var settings = new AppSettings { MaxHistorySize = 3 };
        await _service.SaveSettingsAsync(settings);

        // Act
        for (int i = 0; i < 5; i++)
        {
            var estimate = EstimateResult.Create($"{i} days", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
            await _service.SaveEstimateAsync(estimate);
            await Task.Delay(10);
        }

        var history = await _service.GetHistoryAsync();

        // Assert - Should have only the newest 3
        history.Should().HaveCount(3);
        history[0].EstimateText.Should().Be("4 days"); // Newest
        history[1].EstimateText.Should().Be("3 days");
        history[2].EstimateText.Should().Be("2 days");
        // "0 days" and "1 days" should be pruned
    }

    /// <summary>
    /// Verifies that clearing history removes all stored estimates.
    /// </summary>
    [Fact]
    public async Task ClearHistoryAsync_ShouldRemoveAllEstimates()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var estimate = EstimateResult.Create($"{i} days", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
            await _service.SaveEstimateAsync(estimate);
        }

        // Act
        await _service.ClearHistoryAsync();
        var count = await _service.GetHistoryCountAsync();

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that the history count accurately reflects the number of stored estimates.
    /// </summary>
    [Fact]
    public async Task GetHistoryCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 7; i++)
        {
            var estimate = EstimateResult.Create($"{i} days", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
            await _service.SaveEstimateAsync(estimate);
        }

        // Act
        var count = await _service.GetHistoryCountAsync();

        // Assert
        count.Should().Be(7);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Verifies that estimates with special characters are persisted correctly.
    /// </summary>
    [Fact]
    public async Task SaveEstimateAsync_WithSpecialCharacters_ShouldPersist()
    {
        // Arrange
        var estimate = EstimateResult.Create("when hell freezes over", EstimateMode.Humorous, 0.9, TimeSpan.FromSeconds(20));

        // Act
        await _service.SaveEstimateAsync(estimate);
        var history = await _service.GetHistoryAsync();

        // Assert
        history.Should().ContainSingle();
        history[0].EstimateText.Should().Be("when hell freezes over");
    }

    /// <summary>
    /// Verifies that requesting zero items returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_WithCountZero_ShouldReturnEmptyList()
    {
        // Arrange
        var estimate = EstimateResult.Create("1 day", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        await _service.SaveEstimateAsync(estimate);

        // Act
        var history = await _service.GetHistoryAsync(0);

        // Assert
        history.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a negative count parameter returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_WithNegativeCount_ShouldReturnEmptyList()
    {
        // Arrange
        var estimate = EstimateResult.Create("1 day", EstimateMode.Work, 0.5, TimeSpan.FromSeconds(5));
        await _service.SaveEstimateAsync(estimate);

        // Act
        var history = await _service.GetHistoryAsync(-1);

        // Assert
        history.Should().BeEmpty();
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies that Dispose completes without error.
    /// </summary>
    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_dispose_{Guid.NewGuid()}.db");
        var service = new StorageService(dbPath);

        // Act
        var act = () => ((IDisposable)service).Dispose();

        // Assert
        act.Should().NotThrow();

        // Cleanup
        if (File.Exists(dbPath))
            File.Delete(dbPath);
    }

    /// <summary>
    /// Verifies that calling Dispose twice is safe (idempotent).
    /// </summary>
    [Fact]
    public void Dispose_CalledTwice_ShouldBeSafe()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_dispose2_{Guid.NewGuid()}.db");
        var service = new StorageService(dbPath);

        // Act
        var act = () =>
        {
            ((IDisposable)service).Dispose();
            ((IDisposable)service).Dispose();
        };

        // Assert
        act.Should().NotThrow();

        // Cleanup
        if (File.Exists(dbPath))
            File.Delete(dbPath);
    }

    #endregion
}
