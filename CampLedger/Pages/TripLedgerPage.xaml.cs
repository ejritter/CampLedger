using CampLedger.Models;
using CampLedger.ViewModels;
using CommunityToolkit.Maui.Extensions;

namespace CampLedger.Pages;

public partial class TripLedgerPage : ContentPage
{
    private CancellationTokenSource? _notesSaveCts;
    private bool _suppressRefreshOnAppearing;

    public TripLedgerPage(TripLedgerViewModel viewModel)
    {
        InitializeComponent();
        this.AttachViewModel(viewModel);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private TripLedgerViewModel ViewModel => (TripLedgerViewModel)BindingContext;

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
            ViewModel.RefreshFromHasListCommand.Execute(null);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TripLedgerViewModel.IsLocationPopupOpen) && ViewModel.IsLocationPopupOpen)
        {
            HandleLocationActionAsync(ViewModel.CurrentTripLocation);
        }
    }

    private async void HandleLocationActionAsync(TripLocation? location)
    {
        // Always show the embedded location popup within the app (consistent with TripHistoryPage).
        ShowLocationPopupAsync(location);
    }

    private async Task OpenLocationInDefaultMapAppAsync(TripLocation? location)
    {
        var url = location?.GoogleMapsUrl;

        if (string.IsNullOrWhiteSpace(url))
        {
            url = "https://www.google.com/maps/";
        }

        try
        {
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenLocationInDefaultMapAppAsync error: {ex.Message}\n{ex.StackTrace}");
            await this.GetPresentingPage().DisplayAlertAsync("Error", "Could not open the maps application.", "OK");
        }
    }

    private async void ShowLocationPopupAsync(TripLocation? initialLocation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ShowLocationPopupAsync: Starting");
            var popup = new TripLocationPopupPage(initialLocation, false);

            System.Diagnostics.Debug.WriteLine("ShowLocationPopupAsync: Showing toolkit popup");
            // Suppress the OnAppearing refresh that occurs when the popup closes.
            _suppressRefreshOnAppearing = true;
            // Must show the popup on the actually-attached window page, not "this" - see
            // GetPresentingPage remarks. TripLedgerPage is never attached to a Window (its
            // Content is reparented into the nav bar and the page itself discarded), so its
            // Navigation.Inner is always null. ShowPopupAsync on an unattached page still
            // "succeeds" (NavigationProxy.OnPushModal queues the push and returns an already
            // completed task when Inner is null) but the popup is never actually displayed,
            // and the awaited PopupClosed TaskCompletionSource then never completes - so the
            // call just hangs forever, which is why clicking the location button did nothing.
            await this.GetPresentingPage().ShowPopupAsync(popup);
            // Ensure suppression is cleared after popup returns in case OnAppearing wasn't fired.
            _suppressRefreshOnAppearing = false;

            var selectedLocation = popup.SelectedLocation;
            
            System.Diagnostics.Debug.WriteLine($"ShowLocationPopupAsync: SelectedLocation = {selectedLocation?.LocationName}");
            
            if (selectedLocation != null)
            {
                System.Diagnostics.Debug.WriteLine($"ShowLocationPopupAsync: Setting trip location to {selectedLocation.LocationName}");
                ViewModel.SetTripLocationCommand.Execute(selectedLocation);
            }

            ViewModel.CloseLocationPopupCommand.Execute(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowLocationPopupAsync error: {ex.Message}\n{ex.StackTrace}");
            await this.GetPresentingPage().DisplayAlertAsync("Error", $"Could not show location popup: {ex.Message}", "OK");
            ViewModel.CloseLocationPopupCommand.Execute(null);
        }
    }

    private void OnNotesTextChanged(object sender, TextChangedEventArgs e)
    {
        _notesSaveCts?.Cancel();
        _notesSaveCts = new CancellationTokenSource();
        var token = _notesSaveCts.Token;

        // Use modern async/await for debouncing notes input on the UI thread/ThreadPool properly
        _ = SaveNotesWithDelayAsync(token);
    }

    private async Task SaveNotesWithDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), token);
            if (!token.IsCancellationRequested)
            {
                ViewModel.SaveTrip();
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when typing rapidly, safe to ignore
        }
    }

    private void OnTripSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            ViewModel.SearchCommand.Execute(string.Empty);
        }
    }

    private void OnClearTripSearchClicked(object? sender, EventArgs e)
    {
        TripSearchBar.Text = string.Empty;
        ViewModel.ClearSearchCommand.Execute(null);
    }
}
