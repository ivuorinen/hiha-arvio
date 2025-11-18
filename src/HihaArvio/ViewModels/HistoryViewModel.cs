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
        var estimates = await _storageService.GetHistoryAsync();

        History.Clear();
        foreach (var estimate in estimates)
        {
            History.Add(estimate);
        }

        IsEmpty = History.Count == 0;
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _storageService.ClearHistoryAsync();

        History.Clear();
        IsEmpty = true;
    }
}
