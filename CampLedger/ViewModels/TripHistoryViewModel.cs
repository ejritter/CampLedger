using System.Collections.ObjectModel;
using CampLedger.Models;
using CampLedger.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class TripHistoryViewModel : ViewModelBase
{
    private readonly ICampLedgerStateService _stateService;
    private readonly IToastNotificationService _toastService;
    private readonly ITripDurationValidationService _tripDurationValidationService;

    [ObservableProperty]
    public partial DateTime FilterStartDate { get; set; } = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    public partial DateTime FilterEndDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial bool IsFilterActive { get; set; }

    [ObservableProperty]
    public partial TripRecordViewModel? SelectedLocationTrip { get; set; }

    [ObservableProperty]
    public partial bool IsLocationPopupOpen { get; set; }

    [ObservableProperty]
    public partial TripRecordViewModel? EditingTrip { get; set; }

    [ObservableProperty]
    public partial bool IsEditingTripInFullScreen { get; set; }

    [ObservableProperty]
    public partial bool IsListVisible { get; set; } = true;

    public TripHistoryViewModel(ICampLedgerStateService stateService, IToastNotificationService toastService, ITripDurationValidationService tripDurationValidationService)
    {
        _stateService = stateService;
        _toastService = toastService;
        _tripDurationValidationService = tripDurationValidationService;
        Trips = [];
        FilteredTrips = [];
        Refresh();
    }

    public ObservableCollection<TripRecordViewModel> Trips { get; }

    public ObservableCollection<TripRecordViewModel> FilteredTrips { get; }

    [RelayCommand]
    public void Refresh()
    {
        Trips.Clear();
        foreach (var trip in _stateService.State.TripHistory
                     .Select(t => new TripRecordViewModel(t))
                     .OrderByDescending(t => t.StartDate)
                     .ThenByDescending(t => t.EndDate))
        {
            Trips.Add(trip);
        }

        ClearFilter();
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        IsFilterActive = true;
        FilteredTrips.Clear();
        var start = FilterStartDate.Date;
        var end = FilterEndDate.Date;

        foreach (var trip in Trips)
        {
            var tripStart = trip.StartDate.Date;
            var tripEnd = trip.EndDate.Date;

            if (tripStart <= end && tripEnd >= start)
            {
                FilteredTrips.Add(trip);
            }
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        IsFilterActive = false;
        FilteredTrips.Clear();
        foreach (var trip in Trips)
        {
            FilteredTrips.Add(trip);
        }
    }

    [RelayCommand]
    private void BeginEditNotes(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        record.EditingNotes = record.Notes;
        record.IsEditingNotes = true;
    }

    [RelayCommand]
    private void BeginEditTripDetails(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        // Switch to full-screen edit for a single trip.
        EditingTrip = record;
        IsEditingTripInFullScreen = true;
        IsListVisible = false;
        record.BeginEditTripDetails();
    }

    [RelayCommand]
    private async Task SaveTripDetails(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        if (!_tripDurationValidationService.IsValid(record.EditingStartDate, record.EditingEndDate))
        {
            await _toastService.ShowAsync(_tripDurationValidationService.GetValidationMessage());
            return;
        }

        record.CommitEditTripDetails();
        _stateService.Save();
        // Close full-screen editor if we just saved the editing trip.
        if (EditingTrip == record)
        {
            IsEditingTripInFullScreen = false;
            IsListVisible = true;
            EditingTrip = null;
        }
    }

    [RelayCommand]
    private void CancelEditTripDetails(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        record.CancelEditTripDetails();
        // Close full-screen editor if active for this record.
        if (EditingTrip == record)
        {
            IsEditingTripInFullScreen = false;
            IsListVisible = true;
            EditingTrip = null;
        }
    }

    [RelayCommand]
    private void EditTripLocation(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        SelectedLocationTrip = record;
        // When editing, force the popup to show 'Confirm' rather than the saved location name.
        IsLocationPopupOpen = true;
    }

    [RelayCommand]
    private async Task OpenOrEditLocation(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        if (record.IsEditingTripDetails)
        {
            // open popup to edit
            EditTripLocation(record);
            return;
        }

        // If not editing and there's no location set, do not allow adding one from the read-only view
        if (record.Trip.Location == null)
        {
            await _toastService.ShowAsync("No location has been set for this trip. Click Edit to add a location.");
            return;
        }

        // Open the embedded location popup so users can view the saved map inside the app (mobile-friendly).
        SelectedLocationTrip = record;
        IsLocationPopupOpen = true;
    }


    [RelayCommand]
    private void ClearEditingTripLocation(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }
        record.EditingLocation = null;
        record.Trip.Location = null;
        _stateService.Save();
        record.RefreshLocation();
    }

    [RelayCommand]
    private void ClearLocation(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        record.Trip.Location = null;
        record.EditingLocation = null;
        _stateService.Save();
        record.RefreshLocation();
    }


    [RelayCommand]
    private void CloseLocationPopup()
    {
        IsLocationPopupOpen = false;
    }

    public void ApplySelectedLocation(TripLocation? location)
    {
        if (SelectedLocationTrip == null)
        {
            return;
        }

        if (location == null)
        {
            return;
        }

        if (SelectedLocationTrip.IsEditingTripDetails)
        {
            // When editing trip details, only apply the selected location to the editing buffer.
            // Do not persist to the underlying Trip.Location until the user saves their edits.
            SelectedLocationTrip.EditingLocation = CloneLocation(location);
        }
        else
        {
            // Not editing: persist the selected location to the trip so it displays in read-only view.
            SelectedLocationTrip.Trip.Location = CloneLocation(location);
            SelectedLocationTrip.RefreshLocation();
            _stateService.Save();
        }
    }

    private static TripLocation CloneLocation(TripLocation location)
    {
        return new TripLocation
        {
            LocationName = location.LocationName,
            GoogleMapsUrl = location.GoogleMapsUrl
        };
    }

    [RelayCommand]
    private void SaveNotes(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        record.Trip.Notes = record.EditingNotes;
        record.IsEditingNotes = false;
        record.RefreshNotes();
        _stateService.Save();
    }

    [RelayCommand]
    private void CancelEditNotes(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        record.IsEditingNotes = false;
    }

    [RelayCommand]
    private async Task DeleteTrip(TripRecordViewModel? record)
    {
        if (record is null)
        {
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Trip",
            $"Are you sure you want to delete the trip from {record.TripDurationText}? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        _stateService.State.TripHistory.Remove(record.Trip);
        _stateService.Save();
        Trips.Remove(record);
        FilteredTrips.Remove(record);

        // If the deleted trip was being edited, close editor and restore list visibility.
        if (EditingTrip == record)
        {
            IsEditingTripInFullScreen = false;
            IsListVisible = true;
            EditingTrip = null;
        }
    }

    [RelayCommand]
    private async Task OpenLocation(TripRecordViewModel? record)
    {
        if (record?.Trip.Location == null)
        {
            await Shell.Current.DisplayAlertAsync("No Location", "No location has been set for this trip.", "OK");
            return;
        }

        var locationUrl = record.Trip.Location.GoogleMapsUrl;

        if (string.IsNullOrWhiteSpace(locationUrl))
        {
            await _toastService.ShowAsync("The saved Google Maps link for this trip is empty.");
            return;
        }

        await Launcher.OpenAsync(new Uri(locationUrl));
    }
}
