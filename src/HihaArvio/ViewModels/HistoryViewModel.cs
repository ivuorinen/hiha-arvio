using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HihaArvio.Models;
using HihaArvio.Services.Interfaces;

namespace HihaArvio.ViewModels;

/// <summary>
/// ViewModel for displaying and managing estimate history.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private ObservableCollection<EstimateResult> _history;

    [ObservableProperty]
    private bool _isEmpty;

    public HistoryViewModel(IStorageService storageService)
    {
        _storageService = storageService;
        _history = new ObservableCollection<EstimateResult>();
        _isEmpty = true;
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        try
        {
            var estimates = await _storageService.GetHistoryAsync();

            History = new ObservableCollection<EstimateResult>(estimates);
            IsEmpty = History.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load history: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        try
        {
            await _storageService.ClearHistoryAsync();

            History.Clear();
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear history: {ex.Message}");
        }
    }
}
