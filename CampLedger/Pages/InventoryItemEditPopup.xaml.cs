using CampLedger.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CampLedger.Pages;

public partial class InventoryItemEditPopup : Popup
{
    private readonly InventoryViewModel _viewModel;

    public InventoryItemEditPopup(InventoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.EditDismissRequested += OnEditDismissRequested;
        Closed += OnPopupClosed;
    }

    private async void OnEditDismissRequested(object? sender, EventArgs e)
    {
        _viewModel.EditDismissRequested -= OnEditDismissRequested;
        await CloseAsync(CancellationToken.None);
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        Closed -= OnPopupClosed;
        _viewModel.EditDismissRequested -= OnEditDismissRequested;
        _viewModel.CancelEditCommand.Execute(null);
    }
}
