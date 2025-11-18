using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.ViewModels;

/// <summary>
/// Main ViewModel coordinating shake detection, estimate generation, and display.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IShakeDetectionService _shakeDetectionService;
    private readonly IEstimateService _estimateService;
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private ShakeData _currentShakeData;

    [ObservableProperty]
    private EstimateResult? _currentEstimate;

    [ObservableProperty]
    private EstimateMode _selectedMode;

    private ShakeData? _lastShakeData;

    public MainViewModel(
        IShakeDetectionService shakeDetectionService,
        IEstimateService estimateService,
        IStorageService storageService)
    {
        _shakeDetectionService = shakeDetectionService;
        _estimateService = estimateService;
        _storageService = storageService;

        // Initialize with default shake data
        _currentShakeData = new ShakeData
        {
            IsShaking = false,
            Intensity = 0.0,
            Duration = TimeSpan.Zero
        };

        // Subscribe to shake events
        _shakeDetectionService.ShakeDataChanged += OnShakeDataChanged;

        // Start monitoring
        _shakeDetectionService.StartMonitoring();

        // Load settings
        _ = LoadSettingsAsync();
    }

    partial void OnSelectedModeChanged(EstimateMode value)
    {
        // Save settings when mode changes
        _ = SaveSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _storageService.LoadSettingsAsync();
        SelectedMode = settings.SelectedMode;
    }

    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            SelectedMode = SelectedMode,
            MaxHistorySize = 10 // Default value, will be managed by SettingsViewModel
        };
        await _storageService.SaveSettingsAsync(settings);
    }

    private void OnShakeDataChanged(object? sender, ShakeData shakeData)
    {
        // Update current shake data
        CurrentShakeData = shakeData;

        // Check if shake just stopped (was shaking, now not)
        if (_lastShakeData?.IsShaking == true && !shakeData.IsShaking)
        {
            // Generate and save estimate asynchronously
            _ = GenerateAndSaveEstimateAsync(_lastShakeData);
        }

        // Store for next comparison
        _lastShakeData = shakeData;
    }

    private async Task GenerateAndSaveEstimateAsync(ShakeData shakeData)
    {
        // Generate estimate based on shake data
        var estimate = _estimateService.GenerateEstimate(
            shakeData.Intensity,
            shakeData.Duration,
            SelectedMode);

        // Update current estimate
        CurrentEstimate = estimate;

        // Save to storage
        await _storageService.SaveEstimateAsync(estimate);

        // Reset shake detection for next shake
        _shakeDetectionService.Reset();
    }

    public void Dispose()
    {
        // Unsubscribe from events
        _shakeDetectionService.ShakeDataChanged -= OnShakeDataChanged;

        // Stop monitoring
        _shakeDetectionService.StopMonitoring();
    }
}
