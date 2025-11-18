using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.ViewModels;

/// <summary>
/// ViewModel for managing application settings.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private EstimateMode _selectedMode;

    [ObservableProperty]
    private int _maxHistorySize;

    public SettingsViewModel(IStorageService storageService)
    {
        _storageService = storageService;

        // Load settings
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _storageService.LoadSettingsAsync();
        SelectedMode = settings.SelectedMode;
        MaxHistorySize = settings.MaxHistorySize;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            SelectedMode = SelectedMode,
            MaxHistorySize = MaxHistorySize
        };

        await _storageService.SaveSettingsAsync(settings);
    }
}
