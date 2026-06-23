using CampLedger.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CampLedger.Pages;

public partial class ThemeSelectionPopup : Popup
{
    private readonly ThemeSelectionPopupViewModel _viewModel;

    public ThemeSelectionPopup(ThemeSelectionPopupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
    }

    private async void OnCloseRequested(object? sender, EventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        await CloseAsync(CancellationToken.None);
    }
}
