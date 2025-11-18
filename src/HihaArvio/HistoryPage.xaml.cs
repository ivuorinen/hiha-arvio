using HihaArvio.ViewModels;

namespace HihaArvio;

public partial class HistoryPage : ContentPage
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load history when page appears
        if (BindingContext is HistoryViewModel viewModel)
        {
            await viewModel.LoadHistoryCommand.ExecuteAsync(null);
        }
    }
}
