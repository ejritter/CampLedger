using CampLedger.Pages;
using CampLedger.Services;
using CampLedger.Models;
using CampLedger.ViewModels;
using CommunityToolkit.Maui.Extensions;

namespace CampLedger.Pages;

public partial class TripLedgerPage : ContentPage
{
    private CancellationTokenSource? _notesSaveCts;

    public TripLedgerPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetService<TripLedgerViewModel>();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private TripLedgerViewModel ViewModel => (TripLedgerViewModel)BindingContext;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.RefreshFromHasListCommand.Execute(null);
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
            await DisplayAlert("Error", "Could not open the maps application.", "OK");
        }
    }

    private async void ShowLocationPopupAsync(TripLocation? initialLocation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ShowLocationPopupAsync: Starting");
            var popup = new TripLocationPopupPage(initialLocation, false);

            System.Diagnostics.Debug.WriteLine("ShowLocationPopupAsync: Showing toolkit popup");
            await this.ShowPopupAsync(popup);

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
            await DisplayAlert("Error", $"Could not show location popup: {ex.Message}", "OK");
            ViewModel.CloseLocationPopupCommand.Execute(null);
        }
    }

    private void OnNotesTextChanged(object sender, TextChangedEventArgs e)
    {
        _notesSaveCts?.Cancel();
        _notesSaveCts = new CancellationTokenSource();
        var token = _notesSaveCts.Token;

        Task.Delay(TimeSpan.FromSeconds(2), token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                ViewModel.SaveTrip();
            }
        }, TaskScheduler.Default);
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
