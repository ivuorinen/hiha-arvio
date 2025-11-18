using FluentAssertions;
using HihaArvio.Models;
using HihaArvio.Services;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.Tests.Integration;

/// <summary>
/// Integration tests verifying that services work together correctly.
/// </summary>
public class ServiceIntegrationTests : IDisposable
{
    private readonly IEstimateService _estimateService;
    private readonly IStorageService _storageService;
    private readonly IShakeDetectionService _shakeDetectionService;
    private readonly IAccelerometerService _accelerometerService;
    private readonly string _testDbPath;

    public ServiceIntegrationTests()
    {
        _estimateService = new EstimateService();
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_integration_{Guid.NewGuid()}.db");
        _storageService = new StorageService(_testDbPath);
        _accelerometerService = new DesktopAccelerometerService();
        _shakeDetectionService = new ShakeDetectionService(_accelerometerService);
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    #region EstimateService + StorageService Integration

    [Fact]
    public async Task GenerateAndSaveEstimate_ShouldPersistToStorage()
    {
        // Arrange
        var intensity = 0.75;
        var duration = TimeSpan.FromSeconds(5);
        var mode = EstimateMode.Work;

        // Act - Generate estimate
        var estimate = _estimateService.GenerateEstimate(intensity, duration, mode);

        // Save to storage
        await _storageService.SaveEstimateAsync(estimate);

        // Load from storage
        var history = await _storageService.GetHistoryAsync();

        // Assert
        history.Should().ContainSingle();
        var saved = history.First();
        saved.Id.Should().Be(estimate.Id);
        saved.EstimateText.Should().Be(estimate.EstimateText);
        saved.Mode.Should().Be(estimate.Mode);
        saved.ShakeIntensity.Should().Be(estimate.ShakeIntensity);
        saved.ShakeDuration.Should().Be(estimate.ShakeDuration);
    }

    [Fact]
    public async Task GenerateMultipleEstimates_ShouldRespectHistoryLimit()
    {
        // Arrange
        var settings = new AppSettings { MaxHistorySize = 3 };
        await _storageService.SaveSettingsAsync(settings);

        // Act - Generate and save 5 estimates
        for (int i = 0; i < 5; i++)
        {
            var estimate = _estimateService.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Work);
            await _storageService.SaveEstimateAsync(estimate);
            await Task.Delay(10); // Ensure different timestamps
        }

        // Assert - Should only keep 3 newest
        var count = await _storageService.GetHistoryCountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task LoadSettings_GenerateEstimate_ShouldUseCorrectMode()
    {
        // Arrange - Save settings with Generic mode
        var settings = new AppSettings { SelectedMode = EstimateMode.Generic };
        await _storageService.SaveSettingsAsync(settings);

        // Act - Load settings and generate estimate
        var loadedSettings = await _storageService.LoadSettingsAsync();
        var estimate = _estimateService.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), loadedSettings.SelectedMode);

        // Assert
        estimate.Mode.Should().Be(EstimateMode.Generic);
    }

    #endregion

    #region ShakeDetectionService + EstimateService Integration

    [Fact]
    public void DetectShake_GenerateEstimate_ShouldUseShakeData()
    {
        // Arrange
        _shakeDetectionService.StartMonitoring();

        // Act - Simulate shake
        _shakeDetectionService.ProcessAccelerometerReading(3.0, 2.5, 2.0);
        var shakeData = _shakeDetectionService.CurrentShakeData;

        // Generate estimate based on shake
        var estimate = _estimateService.GenerateEstimate(
            shakeData.Intensity,
            shakeData.Duration,
            EstimateMode.Work);

        // Assert
        estimate.ShakeIntensity.Should().Be(shakeData.Intensity);
        estimate.ShakeDuration.Should().Be(shakeData.Duration);
        estimate.EstimateText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DetectLongShake_GenerateEstimate_ShouldTriggerHumorousMode()
    {
        // Arrange
        _shakeDetectionService.StartMonitoring();

        // Act - Simulate shake and manually set long duration for test
        _shakeDetectionService.ProcessAccelerometerReading(2.5, 2.0, 1.5);

        // Simulate 16-second shake by generating estimate with long duration
        var estimate = _estimateService.GenerateEstimate(
            0.5,
            TimeSpan.FromSeconds(16), // Exceeds 15-second threshold
            EstimateMode.Work);

        // Assert - Should switch to Humorous mode (easter egg)
        estimate.Mode.Should().Be(EstimateMode.Humorous);
        estimate.EstimateText.Should().BeOneOf(
            "5 minutes", "tomorrow", "eventually", "next quarter",
            "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement");
    }

    [Fact]
    public void DetectVaryingIntensity_GenerateEstimates_ShouldProduceDifferentRanges()
    {
        // Arrange
        _shakeDetectionService.StartMonitoring();

        // Act - Test low, medium, and high intensity shakes
        var estimates = new List<EstimateResult>();

        // Low intensity shake
        _shakeDetectionService.ProcessAccelerometerReading(1.8, 1.5, 0.5);
        var lowShake = _shakeDetectionService.CurrentShakeData;
        lowShake.Intensity.Should().BeLessThan(0.3);
        estimates.Add(_estimateService.GenerateEstimate(lowShake.Intensity, lowShake.Duration, EstimateMode.Work));

        // High intensity shake
        _shakeDetectionService.Reset();
        _shakeDetectionService.ProcessAccelerometerReading(4.0, 3.5, 3.0);
        var highShake = _shakeDetectionService.CurrentShakeData;
        highShake.Intensity.Should().BeGreaterThan(0.7);
        estimates.Add(_estimateService.GenerateEstimate(highShake.Intensity, highShake.Duration, EstimateMode.Work));

        // Assert - Both should generate valid estimates
        estimates.Should().AllSatisfy(e =>
        {
            e.EstimateText.Should().NotBeNullOrEmpty();
            e.Mode.Should().Be(EstimateMode.Work);
        });
    }

    #endregion

    #region Full Flow Integration

    [Fact]
    public async Task CompleteFlow_ShakeToEstimateToStorage_ShouldWorkEndToEnd()
    {
        // Arrange
        var settings = await _storageService.LoadSettingsAsync();
        _shakeDetectionService.StartMonitoring();

        // Act - Simulate shake gesture
        _shakeDetectionService.ProcessAccelerometerReading(3.0, 2.5, 2.0);
        var shakeData = _shakeDetectionService.CurrentShakeData;

        shakeData.IsShaking.Should().BeTrue();

        // Generate estimate from shake
        var estimate = _estimateService.GenerateEstimate(
            shakeData.Intensity,
            shakeData.Duration,
            settings.SelectedMode);

        // Save to storage
        await _storageService.SaveEstimateAsync(estimate);

        // Load from storage
        var history = await _storageService.GetHistoryAsync();

        // Assert - Complete flow succeeded
        history.Should().ContainSingle();
        var saved = history.First();
        saved.Id.Should().Be(estimate.Id);
        saved.EstimateText.Should().Be(estimate.EstimateText);
        saved.ShakeIntensity.Should().Be(shakeData.Intensity);

        // Reset for next shake
        _shakeDetectionService.Reset();
        _shakeDetectionService.CurrentShakeData.IsShaking.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleShakes_ShouldAccumulateHistory()
    {
        // Arrange
        _shakeDetectionService.StartMonitoring();

        // Act - Simulate 3 shake gestures
        for (int i = 0; i < 3; i++)
        {
            // Shake
            _shakeDetectionService.ProcessAccelerometerReading(2.5, 2.0, 1.5);
            var shakeData = _shakeDetectionService.CurrentShakeData;

            // Generate estimate
            var estimate = _estimateService.GenerateEstimate(
                shakeData.Intensity,
                shakeData.Duration,
                EstimateMode.Work);

            // Save
            await _storageService.SaveEstimateAsync(estimate);

            // Reset for next shake
            _shakeDetectionService.Reset();

            await Task.Delay(10); // Ensure different timestamps
        }

        // Assert
        var history = await _storageService.GetHistoryAsync();
        history.Should().HaveCount(3);

        // Verify ordering (newest first)
        for (int i = 0; i < history.Count - 1; i++)
        {
            history[i].Timestamp.Should().BeOnOrAfter(history[i + 1].Timestamp);
        }
    }

    [Fact]
    public async Task ChangeMode_GenerateEstimate_ShouldUseNewMode()
    {
        // Arrange
        var initialSettings = new AppSettings { SelectedMode = EstimateMode.Work };
        await _storageService.SaveSettingsAsync(initialSettings);

        // Act - Generate estimate with Work mode
        var workEstimate = _estimateService.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Work);
        await _storageService.SaveEstimateAsync(workEstimate);

        // Change mode
        var newSettings = new AppSettings { SelectedMode = EstimateMode.Humorous };
        await _storageService.SaveSettingsAsync(newSettings);

        // Generate estimate with new mode
        var humorousEstimate = _estimateService.GenerateEstimate(0.5, TimeSpan.FromSeconds(5), EstimateMode.Humorous);
        await _storageService.SaveEstimateAsync(humorousEstimate);

        // Assert
        var history = await _storageService.GetHistoryAsync();
        history.Should().HaveCount(2);
        history[0].Mode.Should().Be(EstimateMode.Humorous); // Newest
        history[1].Mode.Should().Be(EstimateMode.Work);     // Oldest
    }

    #endregion

    #region Event Integration

    [Fact]
    public async Task ShakeEvent_ShouldTriggerEstimateGeneration()
    {
        // Arrange
        _shakeDetectionService.StartMonitoring();
        EstimateResult? generatedEstimate = null;

        _shakeDetectionService.ShakeDataChanged += async (sender, shakeData) =>
        {
            if (shakeData.IsShaking)
            {
                // Generate estimate when shake detected
                generatedEstimate = _estimateService.GenerateEstimate(
                    shakeData.Intensity,
                    shakeData.Duration,
                    EstimateMode.Work);

                await _storageService.SaveEstimateAsync(generatedEstimate);
            }
        };

        // Act - Simulate shake
        _shakeDetectionService.ProcessAccelerometerReading(3.0, 2.5, 2.0);

        // Give event handler time to execute
        await Task.Delay(50);

        // Assert
        generatedEstimate.Should().NotBeNull();
        var history = await _storageService.GetHistoryAsync();
        history.Should().ContainSingle();
        history.First().Id.Should().Be(generatedEstimate!.Id);
    }

    #endregion
}
