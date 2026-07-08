using System.ComponentModel;
using CampLedger.ViewModels;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;

namespace CampLedger.Pages;

public partial class TripHistoryPage : ContentPage
{
    private bool _suppressRefreshOnAppearing;

    public TripHistoryPage(TripHistoryViewModel viewModel)
    {
        InitializeComponent();
        this.AttachViewModel(viewModel);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Editor editor)
        {
            return;
        }

        // Find the TripRecordViewModel bound to the parent DataTemplate
        var vm = editor.BindingContext as TripRecordViewModel;

        if (vm == null)
        {
            // Walk up until we find the parent with the correct binding context
            var parent = editor.Parent;

            while (parent != null && vm == null)
            {
                vm = parent.BindingContext as TripRecordViewModel;
                parent = parent.Parent;
            }
        }

        if (vm != null)
        {
            vm.EditingNotes = e.NewTextValue ?? string.Empty;
        }
    }

    private TripHistoryViewModel ViewModel
    {
        get
        {
            return (TripHistoryViewModel)BindingContext;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_suppressRefreshOnAppearing)
        {
            // Skip a single automatic refresh caused by closing a popup launched from this page.
            _suppressRefreshOnAppearing = false;
        }
        else
        {
            ViewModel.ReloadFromStorage();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TripHistoryViewModel.IsLocationPopupOpen) && ViewModel.IsLocationPopupOpen)
        {
            ShowLocationPopupAsync();
        }
    }

    private async void ShowLocationPopupAsync()
    {
        try
        {
            var isEditing = ViewModel.SelectedLocationTrip?.IsEditingTripDetails ?? false;
            // If editing, open popup without pre-filled location so the button shows 'Confirm'.
            // If not editing, open in view-only mode so the button reads 'Close'.
            var popup = isEditing
                ? new TripLocationPopupPage(null, false)
                : new TripLocationPopupPage(ViewModel.SelectedLocationTrip?.EditingLocation, true);
            // Suppress the OnAppearing refresh that occurs when the popup closes.
            _suppressRefreshOnAppearing = true;
            // Must show the popup on the actually-attached window page, not "this" - see
            // GetPresentingPage remarks. TripHistoryPage is never attached to a Window (its
            // Content is reparented into the nav bar and the page itself discarded), so its
            // Navigation.Inner is always null. ShowPopupAsync on an unattached page still
            // "succeeds" (NavigationProxy.OnPushModal queues the push and returns an already
            // completed task when Inner is null) but the popup is never actually displayed,
            // and the awaited PopupClosed TaskCompletionSource then never completes - so the
            // call just hangs forever, which is why clicking the location button did nothing.
            await this.GetPresentingPage().ShowPopupAsync(popup);
            // Ensure suppression is cleared after popup returns in case OnAppearing wasn't fired.
            _suppressRefreshOnAppearing = false;

            // Only apply when a location was actually selected (Confirm). If the user cancelled the popup,
            // popup.SelectedLocation will be null and we must not modify the editing state.
            if (popup.SelectedLocation != null)
            {
                ViewModel.ApplySelectedLocation(popup.SelectedLocation);
            }

            ViewModel.CloseLocationPopupCommand.Execute(null);


        }
        catch
        {
            ViewModel.CloseLocationPopupCommand.Execute(null);
        }
    }
}
