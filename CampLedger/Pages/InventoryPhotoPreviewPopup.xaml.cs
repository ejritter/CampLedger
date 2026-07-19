using CampLedger.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CampLedger.Pages;

public partial class InventoryPhotoPreviewPopup : Popup
{
    private readonly InventoryViewModel _viewModel;

    public InventoryPhotoPreviewPopup(InventoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PhotoPreviewDismissRequested += OnPhotoPreviewDismissRequested;
        Closed += OnPopupClosed;
    }

    private async void OnPhotoPreviewDismissRequested(object? sender, EventArgs e)
    {
        _viewModel.PhotoPreviewDismissRequested -= OnPhotoPreviewDismissRequested;
        await CloseAsync(CancellationToken.None);
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        Closed -= OnPopupClosed;
        _viewModel.PhotoPreviewDismissRequested -= OnPhotoPreviewDismissRequested;
        _viewModel.ClosePhotoPreviewCommand.Execute(null);
    }
}
